namespace Contracts.Requests;

public record MoveTaskRequest(
    Guid TargetColumnId,
    int NewPosition);