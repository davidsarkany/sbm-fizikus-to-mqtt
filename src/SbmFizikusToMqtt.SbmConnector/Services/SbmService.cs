using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SbmFizikusToMqtt.SbmConnector.Converters;
using SbmFizikusToMqtt.SbmConnector.Exceptions;
using SbmFizikusToMqtt.SbmConnector.Interfaces;
using SbmFizikusToMqtt.SbmConnector.Models.Requests;
using SbmFizikusToMqtt.SbmConnector.Models.Response;

namespace SbmFizikusToMqtt.SbmConnector.Services;

internal sealed class SbmService(IHttpClientFactory httpClientFactory) : ISbmService
{
    private const string DefaultMediaType = "application/json";
    private const string RequestUri = "/frontend";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Converters =
        {
            new CustomDateTimeOffsetConverter(),
            new SbmBuildingAccessRightsResponseConverter()
        },
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SbmClient");

    public async Task<SbmTokenResponse> GetToken(string username, string password,
        CancellationToken cancellationToken = default)
    {
        var request = new SbmTokenRequest(username, HashService.Sha256Hash(password));
        return await SendRequestAsync<SbmTokenRequest, SbmTokenResponse>(request, "login failed", cancellationToken);
    }

    public async Task<SbmGetBuildingAccessRightsResponse[]> GetBuildingAccessRights(string token,
        CancellationToken cancellationToken = default)
    {
        var request = new SbmGetBuildingAccessRightsRequest(token);
        var values = await SendRequestAsync<SbmGetBuildingAccessRightsRequest, SbmGetBuildingAccessRightsResponse[]>(
            request, "Failed to parse building access rights response", cancellationToken);

        if (values.Length == 0)
            throw new SbmInvalidResponseException("SBM returned empty building access rights response");

        return values;
    }

    public async Task<SbmApartmentListResponse[]> GetApartmentList(string buildingId, string token,
        CancellationToken cancellationToken = default)
    {
        var request = new SbmApartmentListRequest(buildingId, token);
        var apartments = await SendRequestAsync<SbmApartmentListRequest, SbmApartmentListResponse[]>(
            request, "Failed to parse apartment list", cancellationToken);

        if (apartments.Length == 0)
            throw new SbmInvalidResponseException("SBM returned empty apartment list");

        return apartments;
    }

    public async Task<SbmChangeTemperatureResponse> ChangeTemperature(int thermostatId, double temperature,
        string token,
        CancellationToken cancellationToken = default)
    {
        var request = new SbmChangeTemperatureRequest(thermostatId, temperature, token);
        return await SendRequestAsync<SbmChangeTemperatureRequest, SbmChangeTemperatureResponse>(
            request, "SbmChangeTemperatureResponse deserialize failed", cancellationToken);
    }

    public async Task<SbmApartmentInfoResponse> GetApartmentInfo(int apartmentId, string token,
        CancellationToken cancellationToken = default)
    {
        var request = new SbmApartmentInfoRequest(apartmentId, token);
        return await SendRequestAsync<SbmApartmentInfoRequest, SbmApartmentInfoResponse>(
            request, "get apartment info failed", cancellationToken);
    }

    private async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        TRequest requestData,
        string errorContext,
        CancellationToken cancellationToken) where TResponse : class
    {
        var content = new StringContent(
            JsonSerializer.Serialize(requestData),
            Encoding.UTF8,
            DefaultMediaType);

        var response = await _httpClient.PutAsync(RequestUri, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"SBM API request failed with status {(int)response.StatusCode} ({response.ReasonPhrase}): {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            var result = JsonSerializer.Deserialize<TResponse>(responseJson, JsonSerializerOptions);
            if (result == null)
                throw new SbmInvalidResponseException($"{errorContext}: {responseJson}");
            return result;
        }
        catch (JsonException ex)
        {
            throw new SbmInvalidResponseException($"{errorContext}: {responseJson}", ex);
        }
    }
}