using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SbmFizikusToMqtt.Application.Configurations;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace SbmFizikusToMqtt.Application.Extensions;

public static class WebApplicationExtensions
{
    public static async Task InitializeSbmPollingAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var options = scope.ServiceProvider
            .GetRequiredService<IOptions<PollingConfiguration>>().Value;
        var manager = scope.ServiceProvider
            .GetRequiredService<ICronTickerManager<CronTickerEntity>>();

        await manager.AddAsync(new CronTickerEntity
        {
            Function = "Polling SBM",
            Expression = options.PollingCronExpression
        });
    }
}