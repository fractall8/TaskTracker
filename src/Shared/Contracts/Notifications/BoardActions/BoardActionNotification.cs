namespace Contracts.Notifications.BoardActions;

public record BoardActionNotification(
    Guid BoardId,
    BoardActionNotificationType Type,
    Guid ActorUserId,
    DateTimeOffset OccurredAt,
    BoardActionPayload Payload);
