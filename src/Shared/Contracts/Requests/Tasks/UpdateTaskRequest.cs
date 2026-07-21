namespace Contracts.Requests.Tasks;

public record UpdateTaskRequest(
    string Title,
    string? Description,
    DateTimeOffset? DueDate,
    Guid? AssigneeId);
