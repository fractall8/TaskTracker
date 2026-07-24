namespace Domain.Exceptions;

public class SubscriptionFeatureRequiredException(string feature)
    : AppException($"This workspace requires an upgraded subscription to use the '{feature}' feature.")
{
    public string Feature { get; } = feature;

    public override int StatusCode => 403;

    public override string Title => "Upgrade Required";
}
