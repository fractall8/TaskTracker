using Contracts.Notifications.BoardActions.Payloads.Positions;
using Domain.Entities;

namespace Application.Common.Mappings;

internal static class BoardActionPositionMappings
{
    public static IReadOnlyList<BoardActionColumnPosition> ToColumnPositions(IEnumerable<Column> columns) =>
        columns
            .OrderBy(column => column.Position)
            .Select(column => new BoardActionColumnPosition(column.Id, column.Position))
            .ToList();

    public static IReadOnlyList<BoardActionTaskPosition> ToTaskPositions(IEnumerable<TaskItem> tasks) =>
        tasks
            .OrderBy(task => task.Position)
            .Select(task => new BoardActionTaskPosition(task.Id, task.ColumnId, task.Position))
            .ToList();
}
