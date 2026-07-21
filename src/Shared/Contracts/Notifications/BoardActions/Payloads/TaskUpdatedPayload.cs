namespace Contracts.Notifications.BoardActions.Payloads;

public record TaskUpdatedPayload(
    Guid ColumnId,
    Guid BoardTaskId,
    string Title,
    string? Description,
    Guid? AssigneeId) : BoardActionPayload;
