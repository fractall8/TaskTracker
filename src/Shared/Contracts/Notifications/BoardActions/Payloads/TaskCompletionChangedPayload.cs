namespace Contracts.Notifications.BoardActions.Payloads;

public record TaskCompletionChangedPayload(
    Guid TaskId,
    bool IsCompleted,
    DateTimeOffset? CompletedAt) : BoardActionPayload;
