namespace Contracts.Requests.Tasks;

public record MoveTaskRequest(
    Guid TargetColumnId,
    int NewPosition);
