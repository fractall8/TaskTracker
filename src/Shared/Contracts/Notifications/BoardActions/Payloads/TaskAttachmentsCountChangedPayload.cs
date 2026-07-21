namespace Contracts.Notifications.BoardActions.Payloads;

public record TaskAttachmentsCountChangedPayload(
    Guid BoardTaskId,
    int AttachmentsCount) : BoardActionPayload;
