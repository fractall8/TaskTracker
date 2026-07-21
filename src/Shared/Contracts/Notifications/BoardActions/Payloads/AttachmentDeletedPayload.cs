namespace Contracts.Notifications.BoardActions.Payloads;

public record AttachmentDeletedPayload(Guid TaskId, Guid AttachmentId) : BoardActionPayload;
