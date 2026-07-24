namespace Domain.Exceptions;

public class BusinessRuleValidationException(string message) : AppException(message)
{
    public override int StatusCode => 400;

    public override string Title => "Business Rule Violation";
}
