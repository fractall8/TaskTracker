using Contracts.Notifications.BoardActions.Payloads.Positions;

namespace Contracts.Notifications.BoardActions.Payloads;

public record ColumnsReorderedPayload(
    IReadOnlyList<BoardActionColumnPosition> Columns) : BoardActionPayload;
