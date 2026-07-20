using Contracts.Notifications.BoardActions.Payloads.Positions;

namespace Contracts.Notifications.BoardActions.Payloads;

public record ColumnDeletedPayload(
    Guid ColumnId,
    IReadOnlyList<BoardActionColumnPosition> RemainingColumns) : BoardActionPayload;
