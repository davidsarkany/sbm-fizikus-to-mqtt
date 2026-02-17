using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using SbmFizikusToMqtt.Application.Configurations;
using SbmFizikusToMqtt.Application.Extensions;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace SbmFizikusToMqtt.Application.Tests.Extensions;

public sealed class WebApplicationExtensionsTests
{
    [Fact]
    public async Task InitializeSbmPollingAsync_ValidConfiguration_AddsCronTicker()
    {
        // Arrange
        const string cronExpression = "*/5 * * * *";
        var cronTickerManagerMock = new Mock<ICronTickerManager<CronTickerEntity>>();

        // Use ReturnsAsync with a factory to create TickerResult
        cronTickerManagerMock
            .Setup(x => x.AddAsync(It.IsAny<CronTickerEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => default!);

        var webApplication = CreateWebApplication(
            cronExpression,
            cronTickerManagerMock.Object);

        // Act
        await webApplication.InitializeSbmPollingAsync();

        // Assert
        cronTickerManagerMock.Verify(
            x => x.AddAsync(
                It.Is<CronTickerEntity>(e =>
                    e.Function == "Polling SBM" &&
                    e.Expression == cronExpression),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InitializeSbmPollingAsync_UsesCorrectFunctionName()
    {
        // Arrange
        const string cronExpression = "*/30 * * * *";
        var cronTickerManagerMock = new Mock<ICronTickerManager<CronTickerEntity>>();
        CronTickerEntity? capturedEntity = null;

        cronTickerManagerMock
            .Setup(x => x.AddAsync(It.IsAny<CronTickerEntity>(), It.IsAny<CancellationToken>()))
            .Callback<CronTickerEntity, CancellationToken>((entity, _) => capturedEntity = entity)
            .ReturnsAsync(() => default!);

        var webApplication = CreateWebApplication(
            cronExpression,
            cronTickerManagerMock.Object);

        // Act
        await webApplication.InitializeSbmPollingAsync();

        // Assert
        Assert.NotNull(capturedEntity);
        Assert.Equal("Polling SBM", capturedEntity.Function);
    }

    [Fact]
    public async Task InitializeSbmPollingAsync_UsesCronExpressionFromConfiguration()
    {
        // Arrange
        const string cronExpression = "0 0 * * *";
        var cronTickerManagerMock = new Mock<ICronTickerManager<CronTickerEntity>>();
        CronTickerEntity? capturedEntity = null;

        cronTickerManagerMock
            .Setup(x => x.AddAsync(It.IsAny<CronTickerEntity>(), It.IsAny<CancellationToken>()))
            .Callback<CronTickerEntity, CancellationToken>((entity, _) => capturedEntity = entity)
            .ReturnsAsync(() => default!);

        var webApplication = CreateWebApplication(
            cronExpression,
            cronTickerManagerMock.Object);

        // Act
        await webApplication.InitializeSbmPollingAsync();

        // Assert
        Assert.NotNull(capturedEntity);
        Assert.Equal(cronExpression, capturedEntity.Expression);
    }

    [Fact]
    public async Task InitializeSbmPollingAsync_DifferentCronExpression_AppliesCorrectly()
    {
        // Arrange
        const string cronExpression = "*/1 * * * *";
        var cronTickerManagerMock = new Mock<ICronTickerManager<CronTickerEntity>>();
        CronTickerEntity? capturedEntity = null;

        cronTickerManagerMock
            .Setup(x => x.AddAsync(It.IsAny<CronTickerEntity>(), It.IsAny<CancellationToken>()))
            .Callback<CronTickerEntity, CancellationToken>((entity, _) => capturedEntity = entity)
            .ReturnsAsync(() => default!);

        var webApplication = CreateWebApplication(
            cronExpression,
            cronTickerManagerMock.Object);

        // Act
        await webApplication.InitializeSbmPollingAsync();

        // Assert
        Assert.NotNull(capturedEntity);
        Assert.Equal(cronExpression, capturedEntity.Expression);
    }

    [Fact]
    public async Task InitializeSbmPollingAsync_CronTickerManagerThrowsException_PropagatesException()
    {
        // Arrange
        const string cronExpression = "*/5 * * * *";
        const string errorMessage = "Failed to add ticker";
        var cronTickerManagerMock = new Mock<ICronTickerManager<CronTickerEntity>>();

        cronTickerManagerMock
            .Setup(x => x.AddAsync(It.IsAny<CronTickerEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(errorMessage));

        var webApplication = CreateWebApplication(
            cronExpression,
            cronTickerManagerMock.Object);

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => webApplication.InitializeSbmPollingAsync());

        Assert.Equal(errorMessage, exception.Message);
    }

    [Fact]
    public async Task InitializeSbmPollingAsync_OptionsServiceThrowsException_PropagatesException()
    {
        // Arrange
        const string cronExpression = "*/5 * * * *";
        const string errorMessage = "Configuration not found";
        var cronTickerManagerMock = new Mock<ICronTickerManager<CronTickerEntity>>();

        var webApplication = CreateWebApplicationWithThrowingOptions(
            cronExpression,
            cronTickerManagerMock.Object,
            errorMessage);

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => webApplication.InitializeSbmPollingAsync());

        Assert.Equal(errorMessage, exception.Message);
    }

    private static WebApplication CreateWebApplication(
        string cronExpression,
        ICronTickerManager<CronTickerEntity> cronTickerManager)
    {
        var builder = WebApplication.CreateBuilder();

        // Configure options using reflection to work with init-only property
        var options = Activator.CreateInstance(typeof(PollingConfiguration), true);
        var property = typeof(PollingConfiguration).GetProperty(nameof(PollingConfiguration.PollingCronExpression));
        property?.SetValue(options, cronExpression);

        var optionsMock = new Mock<IOptions<PollingConfiguration>>();
        optionsMock.Setup(x => x.Value).Returns((PollingConfiguration)options!);

        builder.Services.AddSingleton(optionsMock.Object);
        builder.Services.AddSingleton(cronTickerManager);

        var app = builder.Build();

        return app;
    }

    private static WebApplication CreateWebApplicationWithThrowingOptions(
        string cronExpression,
        ICronTickerManager<CronTickerEntity> cronTickerManager,
        string errorMessage)
    {
        var builder = WebApplication.CreateBuilder();

        // Configure services with throwing options
        var optionsMock = new Mock<IOptions<PollingConfiguration>>();
        optionsMock
            .Setup(x => x.Value)
            .Throws(new InvalidOperationException(errorMessage));

        builder.Services.AddSingleton(optionsMock.Object);
        builder.Services.AddSingleton(cronTickerManager);

        var app = builder.Build();

        return app;
    }
}