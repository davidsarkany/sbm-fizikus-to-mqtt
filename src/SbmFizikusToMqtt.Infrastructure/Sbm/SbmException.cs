using System.Net;

namespace SbmFizikusToMqtt.Infrastructure.Sbm;

/// <summary>An SBM API call failed or returned data the bridge cannot use.</summary>
public class SbmException : Exception
{
    public SbmException(string message) : base(message)
    {
    }

    public SbmException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>SBM answered with a response that could not be parsed or was missing required data.</summary>
public sealed class SbmInvalidResponseException : SbmException
{
    public SbmInvalidResponseException(string message) : base(message)
    {
    }

    public SbmInvalidResponseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>SBM answered with a non-success HTTP status code.</summary>
public sealed class SbmApiException(HttpStatusCode statusCode, string message) : SbmException(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    /// <summary>The token was rejected or has no access; logging in again may fix it.</summary>
    public bool IsAuthenticationFailure => StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
}