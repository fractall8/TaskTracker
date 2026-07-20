using Contracts.Notifications.BoardActions.Payloads.Positions;

namespace Contracts.Notifications.BoardActions.Payloads;

public record ColumnDeletedPayload(
    Guid ColumnId,
    Guid? TargetColumnId,
    IReadOnlyList<BoardActionColumnPosition> RemainingColumns,
    IReadOnlyList<BoardActionTaskPosition>? MovedTasks) : BoardActionPayload;
