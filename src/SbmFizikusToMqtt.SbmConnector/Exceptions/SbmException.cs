namespace SbmFizikusToMqtt.SbmConnector.Exceptions;

public class SbmException : Exception
{
    public SbmException(string message)
        : base(message)
    {
    }

    public SbmException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}