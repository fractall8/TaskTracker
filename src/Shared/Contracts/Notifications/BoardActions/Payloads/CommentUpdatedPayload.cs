namespace Contracts.Notifications.BoardActions.Payloads;

public record CommentUpdatedPayload(Guid TaskId, Guid CommentId, string NewText) : BoardActionPayload;
