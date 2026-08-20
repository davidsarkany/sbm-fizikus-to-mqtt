using SbmFizikusToMqtt.Application.Extensions;
using SbmFizikusToMqtt.Web.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting server.");
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext());

    builder.Services
        .AddApplicationConfigurations(builder.Configuration)
        .AddApplicationServices(builder.Configuration)
        .AddSbmPollingBackgroundService(builder.Configuration);

    var app = builder.Build();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "SBM integration terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}