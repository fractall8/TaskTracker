namespace Contracts.Notifications.BoardActions.Payloads;

public record TaskDetailsUpdatedPayload(
    Guid TaskId,
    string Title,
    string? Description,
    Guid? AssigneeId,
    string? AssigneeName,
    string? AssigneeAvatarUrl) : BoardActionPayload;
