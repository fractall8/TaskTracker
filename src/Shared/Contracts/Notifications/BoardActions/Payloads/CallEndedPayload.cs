namespace Contracts.Notifications.BoardActions.Payloads;

public record CallEndedPayload(Guid BoardCallId) : BoardActionPayload;
