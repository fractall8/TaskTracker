using Contracts.Notifications.BoardActions.Payloads.Positions;

namespace Contracts.Notifications.BoardActions.Payloads;

public record TaskDeletedPayload(
    Guid ColumnId,
    Guid BoardTaskId,
    IReadOnlyList<BoardActionTaskPosition> RemainingTasks) : BoardActionPayload;
