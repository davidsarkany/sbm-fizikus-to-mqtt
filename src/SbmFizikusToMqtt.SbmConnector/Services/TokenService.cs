using Microsoft.Extensions.Options;
using SbmFizikusToMqtt.SbmConnector.Configurations;
using SbmFizikusToMqtt.SbmConnector.Interfaces;
using SbmFizikusToMqtt.SbmConnector.Models.Response;

namespace SbmFizikusToMqtt.SbmConnector.Services;

internal sealed class TokenService(
    ISbmService sbmService,
    TimeProvider timeProvider,
    IOptions<SbmConfiguration> configuration) : ITokenService
{
    private readonly SbmConfiguration _configuration = configuration.Value;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private SbmTokenResponse? _token;

    public async Task<SbmTokenResponse> GetToken(CancellationToken cancellationToken = default)
    {
        // Quick check without lock for performance
        if (HasValidToken())
            return _token!;

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (!HasValidToken())
            {
                var token = await sbmService.GetToken(_configuration.Username, _configuration.Password,
                    cancellationToken);
                _token = token;
            }

            return _token!;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private bool HasValidToken()
    {
        return !(_token == null || _token.Expiration < timeProvider.GetUtcNow());
    }
}