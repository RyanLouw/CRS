using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.HttpOverrides;
using CRS.Core.Helpers;
using CRS.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using CRS.Core.DataAccess;
using CRS.Core.Managers;
using CRS.Core.Models;
using CRS.Core.Notifications;
using CRS.Logging;
using CRS.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        options.Filters.Add<ControllerExceptionFilter>();
    });
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
    builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "roles";
        options.Events.OnRemoteFailure = context =>
        {
            Log.Warning("Entra sign-in failed with {ErrorType}", context.Failure?.GetType().Name);
            context.HandleResponse();
            context.Response.Redirect(context.Request.PathBase + "/Home/AuthenticationError");
            return Task.CompletedTask;
        };
    });
    builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("DirectoryUser", policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
            ClaimsHelpers.GetUserId(context.User).HasValue));
        options.AddPolicy("CallRecording", policy => policy.RequireAuthenticatedUser()
            .RequireAssertion(context => ClaimsHelpers.GetUserId(context.User).HasValue &&
                ClaimsHelpers.CanDownloadRecordings(context.User)));
        options.AddPolicy("CallRatingSystemAdmin", policy => policy.RequireAuthenticatedUser()
            .RequireAssertion(context => ClaimsHelpers.GetUserId(context.User).HasValue)
            .RequireRole("CallRatingSystemAdmin"));
    });
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IUserSession, UserSession>();
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.Configure<CallApiOptions>(builder.Configuration.GetSection("CallApi"));
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = builder.Configuration.GetValue<int?>("ReverseProxy:ForwardLimit") ?? 1;
        foreach (var address in builder.Configuration.GetSection("ReverseProxy:TrustedProxies").Get<string[]>() ?? [])
            options.KnownProxies.Add(IPAddress.Parse(address));
        foreach (var network in builder.Configuration.GetSection("ReverseProxy:TrustedNetworks").Get<string[]>() ?? [])
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
    });
    builder.Services.AddScoped<ICallManager, CallManager>();
    builder.Services.AddScoped<IAdminManager, AdminManager>();
    builder.Services.AddDbContext<CallRatingContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("CallRating")));
    builder.Services.AddDbContext<ADUsersContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("Users")));
    builder.Services.AddScoped<IRatingRepository, CallRatingDataAccess>();
    builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
    builder.Services.AddScoped<IRatingNotifier, SmtpRatingNotifier>();
    // The owner will add CentralConfig bearer-token acquisition to this HttpClient.
    builder.Services.AddHttpClient<CallApiDataAccess>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["CallApi:BaseUrl"]
            ?? throw new InvalidOperationException("Configure CallApi:BaseUrl."));
        client.Timeout = TimeSpan.FromSeconds(30);
    }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

    var app = builder.Build();
    app.Use(async (context, next) =>
    {
        context.Items["OriginalRemoteIpAddress"] = context.Connection.RemoteIpAddress?.ToString();
        await next();
    });
    app.UseForwardedHeaders();
    if (!app.Environment.IsDevelopment()) app.UseHsts();
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseMiddleware<ControllerLoggingMiddleware>();
    app.UseAuthentication();
    app.UseStatusCodePagesWithReExecute("/Home/AccessDenied");
    app.Use(async (context, next) =>
    {
        await next();
        // Render the access-denied page only for 403; preserve other error responses.
        if (context.Response.StatusCode != StatusCodes.Status403Forbidden &&
            context.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodePagesFeature>() is { } pages)
            pages.Enabled = false;
    });
    app.UseAuthorization();
    app.MapPost("/account/logout", async Task<IResult> (HttpContext context, IAntiforgery antiforgery) =>
    {
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException) { return Results.BadRequest("The sign-out request could not be validated."); }
        return Results.SignOut(new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            RedirectUri = builder.Configuration["AzureAd:PostLogoutRedirectUri"] ?? "/Home/SignedOut"
        }, [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
    }).RequireAuthorization();
    app.MapStaticAssets();
    app.MapControllerRoute("default", "{controller=CallList}/{action=Index}/{id?}").WithStaticAssets();
    Log.Information("CallRatingSystem starting");
    app.Run();
}
catch (Exception error)
{
    Log.Fatal(error, "CallRatingSystem terminated unexpectedly");
    throw;
}
finally { Log.CloseAndFlush(); }

public partial class Program;
