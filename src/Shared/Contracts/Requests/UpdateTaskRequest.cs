namespace Contracts.Requests;

public record UpdateTaskRequest(
    string Title,
    string? Description,
    DateTimeOffset? DueDate,
    Guid? AssigneeId,
    Guid ColumnId);
