using CRS.Core.Data;
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
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddScoped<ICallManager, CallManager>();
    builder.Services.AddScoped<IAdminManager, AdminManager>();
    builder.Services.AddDbContext<CallRatingContext>(options =>
        options.UseSqlServer(builder.Configuration["Database:CallRating"]));
    builder.Services.AddDbContext<ADUsersContext>(options =>
        options.UseSqlServer(builder.Configuration["Database:Users"]));
    builder.Services.AddScoped<IRatingRepository, CallRatingDataAccess>();
    builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
    builder.Services.AddScoped<IRatingNotifier, SmtpRatingNotifier>();
    builder.Services.AddHttpClient<CallApiDataAccess>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["CallApi:BaseUrl"]
            ?? throw new InvalidOperationException("Configure CallApi:BaseUrl."));
        client.Timeout = TimeSpan.FromSeconds(30);
    }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

    var app = builder.Build();
    if (!app.Environment.IsDevelopment()) app.UseHsts();
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseMiddleware<ControllerLoggingMiddleware>();
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
