namespace Domain.Exceptions;

public class ConflictException(string message) : AppException(message)
{
    public override int StatusCode => 409;

    public override string Title => "Conflict";
}
