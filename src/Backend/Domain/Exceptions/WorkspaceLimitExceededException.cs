namespace Domain.Exceptions;

public class WorkspaceLimitExceededException(string limitName, int limit)
    : AppException(
        $"The workspace has reached its plan limit of {limit} for {limitName}. Upgrade your subscription to increase this limit.")
{
    public string LimitName { get; } = limitName;

    public int Limit { get; } = limit;

    public override int StatusCode => 403;

    public override string Title => "Subscription Limit Reached";
}
