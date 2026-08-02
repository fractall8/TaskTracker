namespace Domain.Exceptions;

public class ExternalServiceUnavailableException : AppException
{
    public ExternalServiceUnavailableException(string message) : base(message)
    {
    }

    public ExternalServiceUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public override int StatusCode => 503;

    public override string Title => "Service Unavailable";
}
