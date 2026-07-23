namespace Domain.Exceptions;

public class NotFoundException(string message) : AppException(message)
{
    public override int StatusCode => 404;

    public override string Title => "Not Found";
}
