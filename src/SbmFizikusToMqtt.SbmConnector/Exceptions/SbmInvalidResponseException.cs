namespace SbmFizikusToMqtt.SbmConnector.Exceptions;

public sealed class SbmInvalidResponseException : SbmException
{
    public SbmInvalidResponseException(string message)
        : base(message)
    {
    }

    public SbmInvalidResponseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}