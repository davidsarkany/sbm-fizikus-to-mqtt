using SbmFizikusToMqtt.SbmConnector.Models.Response;

namespace SbmFizikusToMqtt.SbmConnector.Interfaces;

internal interface ITokenService
{
    public Task<SbmTokenResponse> GetToken(CancellationToken cancellationToken = default);
}