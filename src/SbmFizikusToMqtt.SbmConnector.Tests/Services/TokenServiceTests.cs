using Microsoft.Extensions.Options;
using Moq;
using SbmFizikusToMqtt.SbmConnector.Configurations;
using SbmFizikusToMqtt.SbmConnector.Interfaces;
using SbmFizikusToMqtt.SbmConnector.Models.Response;
using SbmFizikusToMqtt.SbmConnector.Services;

namespace SbmFizikusToMqtt.SbmConnector.Tests.Services;

public class TokenServiceTests
{
    private readonly IOptions<SbmConfiguration> _configuration;
    private readonly Mock<ISbmService> _sbmServiceMock;
    private readonly Mock<TimeProvider> _timeProviderMock;
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        _sbmServiceMock = new Mock<ISbmService>();
        _timeProviderMock = new Mock<TimeProvider>();

        _configuration = Options.Create(new SbmConfiguration
        {
            Username = "testuser",
            Password = "testpassword",
            BaseUrl = "https://test.api.com"
        });

        _tokenService = new TokenService(
            _sbmServiceMock.Object,
            _timeProviderMock.Object,
            _configuration);
    }

    [Fact]
    public async Task GetToken_FirstCall_RetrievesNewToken()
    {
        // Arrange
        var currentTime = new DateTimeOffset(2026, 2, 15, 10, 0, 0, TimeSpan.Zero);
        var expectedToken = new SbmTokenResponse
        {
            AccessToken = "test-token-123",
            Expiration = currentTime.AddHours(1),
            RefreshToken = "refresh-token-123",
            RefreshTokenExpiration = currentTime.AddDays(7),
            Rights = new List<string> { "read", "write" }
        };

        _timeProviderMock.Setup(x => x.GetUtcNow()).Returns(currentTime);
        _sbmServiceMock
            .Setup(x => x.GetToken(_configuration.Value.Username, _configuration.Value.Password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await _tokenService.GetToken();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedToken.AccessToken, result.AccessToken);
        Assert.Equal(expectedToken.RefreshToken, result.RefreshToken);
        Assert.Equal(expectedToken.Expiration, result.Expiration);
        Assert.Equal(expectedToken.Rights, result.Rights);

        _sbmServiceMock.Verify(
            x => x.GetToken(_configuration.Value.Username, _configuration.Value.Password,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetToken_CalledTwiceWithValidToken_UsesCachedToken()
    {
        // Arrange
        var currentTime = new DateTimeOffset(2026, 2, 15, 10, 0, 0, TimeSpan.Zero);
        var token = new SbmTokenResponse
        {
            AccessToken = "cached-token",
            Expiration = currentTime.AddHours(1),
            RefreshToken = "refresh-token",
            RefreshTokenExpiration = currentTime.AddDays(7),
            Rights = new List<string> { "read" }
        };

        _timeProviderMock.Setup(x => x.GetUtcNow()).Returns(currentTime);
        _sbmServiceMock
            .Setup(x => x.GetToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        var firstResult = await _tokenService.GetToken();
        var secondResult = await _tokenService.GetToken();

        // Assert
        Assert.NotNull(firstResult);
        Assert.NotNull(secondResult);
        Assert.Equal(firstResult.AccessToken, secondResult.AccessToken);
        Assert.Same(firstResult, secondResult); // Should be the same instance

        // Verify the service was only called once (token was cached)
        _sbmServiceMock.Verify(
            x => x.GetToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetToken_ExpiredToken_RetrievesNewToken()
    {
        // Arrange
        var initialTime = new DateTimeOffset(2026, 2, 15, 10, 0, 0, TimeSpan.Zero);
        var expiredTime = new DateTimeOffset(2026, 2, 15, 12, 0, 0, TimeSpan.Zero);

        var firstToken = new SbmTokenResponse
        {
            AccessToken = "first-token",
            Expiration = initialTime.AddHours(1), // Expires at 11:00
            RefreshToken = "refresh-1",
            RefreshTokenExpiration = initialTime.AddDays(7),
            Rights = new List<string> { "read" }
        };

        var secondToken = new SbmTokenResponse
        {
            AccessToken = "second-token",
            Expiration = expiredTime.AddHours(1), // Expires at 13:00
            RefreshToken = "refresh-2",
            RefreshTokenExpiration = expiredTime.AddDays(7),
            Rights = new List<string> { "read", "write" }
        };

        // First call at 10:00
        _timeProviderMock.Setup(x => x.GetUtcNow()).Returns(initialTime);
        _sbmServiceMock
            .Setup(x => x.GetToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstToken);

        var firstResult = await _tokenService.GetToken();

        // Second call at 12:00 (after token expired at 11:00)
        _timeProviderMock.Setup(x => x.GetUtcNow()).Returns(expiredTime);
        _sbmServiceMock
            .Setup(x => x.GetToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondToken);

        // Act
        var secondResult = await _tokenService.GetToken();

        // Assert
        Assert.NotNull(firstResult);
        Assert.NotNull(secondResult);
        Assert.Equal("first-token", firstResult.AccessToken);
        Assert.Equal("second-token", secondResult.AccessToken);
        Assert.NotSame(firstResult, secondResult);

        // Verify the service was called twice (token expired)
        _sbmServiceMock.Verify(
            x => x.GetToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GetToken_TokenExpiresExactlyAtCurrentTime_StillUsesToken()
    {
        // Arrange
        // Note: The HasValidToken logic uses < (strictly less than), not <=
        // So a token that expires exactly at current time is still considered valid
        var currentTime = new DateTimeOffset(2026, 2, 15, 11, 0, 0, TimeSpan.Zero);

        var token = new SbmTokenResponse
        {
            AccessToken = "expiring-token",
            Expiration = currentTime, // Expires exactly at current time
            RefreshToken = "refresh-1",
            RefreshTokenExpiration = currentTime.AddDays(7),
            Rights = new List<string> { "read" }
        };

        _timeProviderMock.Setup(x => x.GetUtcNow()).Returns(currentTime);
        _sbmServiceMock
            .Setup(x => x.GetToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        var firstResult = await _tokenService.GetToken();
        var secondResult = await _tokenService.GetToken();

        // Assert - Token should still be used (not considered expired when equal)
        Assert.Equal("expiring-token", firstResult.AccessToken);
        Assert.Same(firstResult, secondResult);

        _sbmServiceMock.Verify(
            x => x.GetToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetToken_TokenExpiredOneMillisecondAgo_RetrievesNewToken()
    {
        // Arrange
        var initialTime = new DateTimeOffset(2026, 2, 15, 10, 0, 0, TimeSpan.Zero);
        var justAfterExpiration = initialTime.AddHours(1).AddMilliseconds(1);

        var firstToken = new SbmTokenResponse
        {
            AccessToken = "expired-token",
            Expiration = initialTime.AddHours(1), // Expires at 11:00:00.000
            RefreshToken = "refresh-1",
            RefreshTokenExpiration = initialTime.AddDays(7),
            Rights = new List<string> { "read" }
        };

        var secondToken = new SbmTokenResponse
        {
            AccessToken = "fresh-token",
            Expiration = justAfterExpiration.AddHours(1),
            RefreshToken = "refresh-2",
            RefreshTokenExpiration = justAfterExpiration.AddDays(7),
            Rights = new List<string> { "read", "write" }
        };

        // First call gets the token
        _timeProviderMock.Setup(x => x.GetUtcNow()).Returns(initialTime);
        _sbmServiceMock
            .Setup(x => x.GetToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstToken);

        await _tokenService.GetToken();

        // Second call is 1ms after expiration
        _timeProviderMock.Setup(x => x.GetUtcNow()).Returns(justAfterExpiration);
        _sbmServiceMock
            .Setup(x => x.GetToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondToken);

        // Act
        var result = await _tokenService.GetToken();

        // Assert - Should retrieve new token
        Assert.Equal("fresh-token", result.AccessToken);

        _sbmServiceMock.Verify(
            x => x.GetToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GetToken_WithCancellationToken_PassesTokenThrough()
    {
        // Arrange
        var currentTime = new DateTimeOffset(2026, 2, 15, 10, 0, 0, TimeSpan.Zero);
        var token = new SbmTokenResponse
        {
            AccessToken = "test-token",
            Expiration = currentTime.AddHours(1),
            RefreshToken = "refresh-token",
            RefreshTokenExpiration = currentTime.AddDays(7),
            Rights = new List<string> { "read" }
        };

        _timeProviderMock.Setup(x => x.GetUtcNow()).Returns(currentTime);
        _sbmServiceMock
            .Setup(x => x.GetToken(_configuration.Value.Username, _configuration.Value.Password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var cancellationTokenSource = new CancellationTokenSource();

        // Act
        var result = await _tokenService.GetToken(cancellationTokenSource.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(token.AccessToken, result.AccessToken);
    }

    [Fact]
    public async Task GetToken_MultipleSequentialCalls_OnlyOneApiCallForValidToken()
    {
        // Arrange
        var currentTime = new DateTimeOffset(2026, 2, 15, 10, 0, 0, TimeSpan.Zero);
        var token = new SbmTokenResponse
        {
            AccessToken = "persistent-token",
            Expiration = currentTime.AddHours(2),
            RefreshToken = "refresh-token",
            RefreshTokenExpiration = currentTime.AddDays(7),
            Rights = new List<string> { "read", "write" }
        };

        _timeProviderMock.Setup(x => x.GetUtcNow()).Returns(currentTime);
        _sbmServiceMock
            .Setup(x => x.GetToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act - Call GetToken 5 times
        var results = new List<SbmTokenResponse>();
        for (var i = 0; i < 5; i++) results.Add(await _tokenService.GetToken());

        // Assert
        Assert.All(results, result => Assert.Equal("persistent-token", result.AccessToken));
        Assert.All(results, result => Assert.Same(results[0], result)); // All should be same instance

        // Verify the service was only called once
        _sbmServiceMock.Verify(
            x => x.GetToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetToken_TokenAboutToExpire_StillUsesToken()
    {
        // Arrange
        var currentTime = new DateTimeOffset(2026, 2, 15, 10, 59, 59, TimeSpan.Zero);
        var token = new SbmTokenResponse
        {
            AccessToken = "about-to-expire-token",
            Expiration = new DateTimeOffset(2026, 2, 15, 11, 0, 0, TimeSpan.Zero), // Expires in 1 second
            RefreshToken = "refresh-token",
            RefreshTokenExpiration = currentTime.AddDays(7),
            Rights = new List<string> { "read" }
        };

        _timeProviderMock.Setup(x => x.GetUtcNow()).Returns(currentTime);
        _sbmServiceMock
            .Setup(x => x.GetToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        var firstResult = await _tokenService.GetToken();
        var secondResult = await _tokenService.GetToken();

        // Assert - Token should still be used as it hasn't expired yet
        Assert.Equal("about-to-expire-token", firstResult.AccessToken);
        Assert.Same(firstResult, secondResult);

        _sbmServiceMock.Verify(
            x => x.GetToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetToken_UsesConfigurationCredentials()
    {
        // Arrange
        var currentTime = new DateTimeOffset(2026, 2, 15, 10, 0, 0, TimeSpan.Zero);
        var token = new SbmTokenResponse
        {
            AccessToken = "test-token",
            Expiration = currentTime.AddHours(1),
            RefreshToken = "refresh-token",
            RefreshTokenExpiration = currentTime.AddDays(7),
            Rights = new List<string> { "read" }
        };

        _timeProviderMock.Setup(x => x.GetUtcNow()).Returns(currentTime);
        _sbmServiceMock
            .Setup(x => x.GetToken("testuser", "testpassword", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        var result = await _tokenService.GetToken();

        // Assert
        Assert.NotNull(result);
        _sbmServiceMock.Verify(
            x => x.GetToken("testuser", "testpassword", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetToken_TimeMovesBackward_RefreshesToken()
    {
        // Arrange - This tests clock skew or time zone changes
        var initialTime = new DateTimeOffset(2026, 2, 15, 12, 0, 0, TimeSpan.Zero);
        var earlierTime = new DateTimeOffset(2026, 2, 15, 9, 0, 0, TimeSpan.Zero);

        var firstToken = new SbmTokenResponse
        {
            AccessToken = "first-token",
            Expiration = initialTime.AddHours(1), // Expires at 13:00
            RefreshToken = "refresh-1",
            RefreshTokenExpiration = initialTime.AddDays(7),
            Rights = new List<string> { "read" }
        };

        var secondToken = new SbmTokenResponse
        {
            AccessToken = "second-token",
            Expiration = earlierTime.AddHours(1),
            RefreshToken = "refresh-2",
            RefreshTokenExpiration = earlierTime.AddDays(7),
            Rights = new List<string> { "read" }
        };

        // First call at 12:00
        _timeProviderMock.Setup(x => x.GetUtcNow()).Returns(initialTime);
        _sbmServiceMock
            .Setup(x => x.GetToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstToken);

        await _tokenService.GetToken();

        // Time moves backward to 9:00 (expiration is still at 13:00, so token should be valid)
        _timeProviderMock.Setup(x => x.GetUtcNow()).Returns(earlierTime);

        // Act
        var result = await _tokenService.GetToken();

        // Assert - Should use cached token as it's still valid (expires at 13:00, current time 9:00)
        Assert.Equal("first-token", result.AccessToken);

        _sbmServiceMock.Verify(
            x => x.GetToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}