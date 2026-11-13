using CRS.Core.Api;
using CRS.Core.Data;
using CRS.Core.Demo;
using CRS.Core.Managers;
using CRS.Core.Models;
using CRS.Core.Notifications;
using CRS.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using CRS.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddSerilog((services, logger) =>
        ApplicationLogging.Configure(logger, builder.Configuration, builder.Environment));
    builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        options.Filters.Add<WorkflowExceptionFilter>();
    });
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddScoped<ICallManager, CallManager>();
    builder.Services.AddScoped<IAdminManager, AdminManager>();
    var demo = builder.Configuration.GetValue<bool>("Demo:Enabled");
    if (demo && !builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Demo mode is allowed only in Development.");
    if (demo)
    {
        builder.Services.AddSingleton<DemoServices>();
        builder.Services.AddSingleton<IRatingRepository>(s => s.GetRequiredService<DemoServices>());
        builder.Services.AddSingleton<ICallApi>(s => s.GetRequiredService<DemoServices>());
        builder.Services.AddSingleton<IRatingNotifier>(s => s.GetRequiredService<DemoServices>());
    }
    else
    {
        builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));
        builder.Services.Configure<CallApiOptions>(builder.Configuration.GetSection("CallApi"));
        builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
        builder.Services.AddScoped<ISqlExecutor, SqlExecutor>();
        builder.Services.AddScoped<IRatingRepository, SqlRatingRepository>();
        builder.Services.AddScoped<IRatingNotifier, SmtpRatingNotifier>();
        builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
        builder.Services.AddScoped<IAccessTokenProvider, ConfiguredAccessTokenProvider>();
        builder.Services.AddHttpClient<ICallApi, CallApiClient>((services, client) =>
        {
            var options = services.GetRequiredService<IOptions<CallApiOptions>>().Value;
            if (!Uri.TryCreate(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))
                throw new DependencyException("An HTTPS calls API base URL must be configured.");
            client.BaseAddress = uri;
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 1, 120));
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
    }
    var authentication = builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    }).AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Denied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
    if (!demo)
    {
        var tenant = builder.Configuration["AzureAd:TenantId"];
        var clientId = builder.Configuration["AzureAd:ClientId"];
        if (!Guid.TryParse(tenant, out _) || !Guid.TryParse(clientId, out _))
            throw new InvalidOperationException("Configure AzureAd:TenantId and AzureAd:ClientId, or enable Development demo mode.");
        authentication.AddOpenIdConnect(options =>
        {
            options.Authority = $"https://login.microsoftonline.com/{tenant}/v2.0";
            options.ClientId = clientId;
            options.ClientSecret = builder.Configuration["AzureAd:ClientSecret"];
            options.ResponseType = "code";
            options.UsePkce = true;
            options.MapInboundClaims = false;
            options.TokenValidationParameters.NameClaimType = "name";
            options.TokenValidationParameters.RoleClaimType = "roles";
            options.TokenValidationParameters.ValidateIssuer = true;
            options.CallbackPath = "/signin-oidc";
            options.SaveTokens = false;
            options.Events.OnRemoteFailure = context =>
            {
                context.HandleResponse();
                context.Response.Redirect("/Account/Denied");
                return Task.CompletedTask;
            };
        });
    }
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("HelpDesk", policy => policy.RequireAuthenticatedUser().RequireRole("IT", "Admin"));
        options.AddPolicy("Recordings", policy => policy.RequireAuthenticatedUser().RequireRole("PSP", "IT", "Admin"));
    });

    var app = builder.Build();
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.Use(async (context, next) =>
    {
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            await next(context);
            Log.Information("HTTP completed {StatusCode} in {ElapsedMs} ms; trace {TraceId}",
                context.Response.StatusCode,
                System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                context.TraceIdentifier);
        }
        catch (Exception error)
        {
            Log.Error("HTTP failed with {ErrorType}; trace {TraceId}",
                error.GetType().Name, context.TraceIdentifier);
            throw;
        }
    });
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapStaticAssets();
    app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();
    Log.Information("CallRatingSystem starting");
    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "CallRatingSystem terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
