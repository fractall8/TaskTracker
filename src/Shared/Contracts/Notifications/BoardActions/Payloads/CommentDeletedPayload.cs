namespace Contracts.Notifications.BoardActions.Payloads;

public record CommentDeletedPayload(Guid TaskId, Guid CommentId) : BoardActionPayload;
