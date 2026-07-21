namespace Contracts.Notifications.BoardActions.Payloads;

public record TaskCommentsCountChangedPayload(
    Guid BoardTaskId,
    int CommentsCount) : BoardActionPayload;
