namespace Contracts.Requests;

public record CreateTaskRequest(
    string Title,
    string? Description,
    DateTimeOffset? DueDate,
    Guid? AssigneeId);
