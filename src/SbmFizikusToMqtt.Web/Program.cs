using SbmFizikusToMqtt.Application.Extensions;
using SbmFizikusToMqtt.Web.Extensions;
using TickerQ.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services
    .AddApplicationConfigurations(builder.Configuration)
    .AddApplicationServices(builder.Configuration)
    .AddTickerQServices();

var app = builder.Build();

app.UseTickerQ();
await app.InitializeSbmPollingAsync();
await app.RunAsync();