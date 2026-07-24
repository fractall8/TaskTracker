namespace Domain.Exceptions;

public class ForbiddenException(string message) : AppException(message)
{
    public override int StatusCode => 403;

    public override string Title => "Forbidden";
}
