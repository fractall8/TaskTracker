namespace Contracts.Notifications.BoardActions.Payloads;

public record ColumnCreatedPayload(
    Guid ColumnId,
    string Name,
    int Position) : BoardActionPayload;
