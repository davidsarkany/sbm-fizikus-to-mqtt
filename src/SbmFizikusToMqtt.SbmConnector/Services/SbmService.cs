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
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SbmClient");

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        Converters =
        {
            new CustomDateTimeOffsetConverter(),
            new SbmBuildingAccessRightsResponseConverter()
        },
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public async Task<SbmTokenResponse> GetToken(string username, string password,
        CancellationToken cancellationToken = default)
    {
        var loginData = JsonSerializer.Serialize(new SbmTokenRequest(username, HashService.Sha256Hash(password)));
        var content = new StringContent(loginData, Encoding.UTF8, DefaultMediaType);
        var response = await _httpClient.PutAsync(RequestUri, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            var token = JsonSerializer.Deserialize<SbmTokenResponse>(responseJson, _jsonSerializerOptions);
            if (token == null)
                throw new SbmInvalidResponseException("SbmTokenService returned null response");
            return token;
        }
        catch (JsonException ex)
        {
            throw new SbmInvalidResponseException($"login failed: {responseJson}", ex);
        }
    }

    public async Task<SbmGetBuildingAccessRightsResponse[]> GetBuildingAccessRights(string token,
        CancellationToken cancellationToken = default)
    {
        var requestData = new SbmGetBuildingAccessRightsRequest(token);
        var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, DefaultMediaType);
        var response = await _httpClient.PutAsync(RequestUri, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonArray = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            var values =
                JsonSerializer.Deserialize<SbmGetBuildingAccessRightsResponse[]>(jsonArray, _jsonSerializerOptions);
            if (values == null || values.Length == 0)
                throw new SbmInvalidResponseException(
                    $"SBM returned empty building access rights response: {jsonArray}");

            return values;
        }
        catch (JsonException ex)
        {
            throw new SbmInvalidResponseException(
                $"Failed to parse building access rights response: {ex.Message}", ex);
        }
    }

    public async Task<SbmApartmentListResponse[]> GetApartmentList(string buildingId, string token,
        CancellationToken cancellationToken = default)
    {
        var requestData = new SbmApartmentListRequest(buildingId, token);
        var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, DefaultMediaType);
        var response = await _httpClient.PutAsync(RequestUri, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonArray = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
            var apartments = JsonSerializer.Deserialize<SbmApartmentListResponse[]>(jsonArray, _jsonSerializerOptions);
            if (apartments == null || apartments.Length == 0)
                throw new SbmInvalidResponseException($"SBM returned empty apartment list: {jsonArray}");

            return apartments;
        }
        catch (JsonException ex)
        {
            throw new SbmInvalidResponseException($"Failed to parse apartment list: {ex.Message}", ex);
        }
    }

    public async Task<SbmChangeTemperatureResponse> ChangeTemperature(int thermostatId, double temperature,
        string token,
        CancellationToken cancellationToken = default)
    {
        var request = new SbmChangeTemperatureRequest(thermostatId, temperature, token);
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, DefaultMediaType);

        var response = await _httpClient.PutAsync(RequestUri, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            var des = JsonSerializer.Deserialize<SbmChangeTemperatureResponse>(responseJson, _jsonSerializerOptions);
            if (des == null)
                throw new SbmInvalidResponseException(
                    $"SbmChangeTemperatureResponse deserialize failed input: {responseJson}");
            return des;
        }
        catch (JsonException ex)
        {
            throw new SbmInvalidResponseException($"Invalid JSON: {responseJson}", ex);
        }
    }

    public async Task<SbmApartmentInfoResponse> GetApartmentInfo(int apartmentId, string token,
        CancellationToken cancellationToken = default)
    {
        var loginData = JsonSerializer.Serialize(new SbmApartmentInfoRequest(apartmentId, token));
        var content = new StringContent(loginData, Encoding.UTF8, DefaultMediaType);

        var response = await _httpClient.PutAsync(RequestUri, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            var apartmentInfo =
                JsonSerializer.Deserialize<SbmApartmentInfoResponse>(responseJson, _jsonSerializerOptions);
            if (apartmentInfo == null)
                throw new SbmInvalidResponseException($"get apartment info failed: {responseJson}");

            return apartmentInfo;
        }
        catch (JsonException ex)
        {
            throw new SbmInvalidResponseException($"Invalid JSON: {responseJson}", ex);
        }
    }
}