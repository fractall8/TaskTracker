namespace Contracts.Notifications.BoardActions.Payloads;

public record TaskCreatedPayload(
    Guid ColumnId,
    Guid BoardTaskId,
    string Title,
    int Position,
    Guid? AssigneeId) : BoardActionPayload;
