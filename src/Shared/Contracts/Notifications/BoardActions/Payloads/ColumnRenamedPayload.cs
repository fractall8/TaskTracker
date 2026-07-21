namespace Contracts.Notifications.BoardActions.Payloads;

public record ColumnRenamedPayload(
    Guid ColumnId,
    string Name) : BoardActionPayload;
