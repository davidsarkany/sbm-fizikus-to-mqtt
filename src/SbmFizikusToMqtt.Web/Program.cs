using SbmFizikusToMqtt.Application.Extensions;
using SbmFizikusToMqtt.Web.Extensions;
using Serilog;
using TickerQ.DependencyInjection;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting server.");
    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddEnvironmentVariables();
    builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext());

    builder.Services
        .AddApplicationConfigurations(builder.Configuration)
        .AddApplicationServices(builder.Configuration)
        .AddTickerQServices()
        .AddSbmPollingAsync(builder.Configuration);

    var app = builder.Build();

    app.UseTickerQ();
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