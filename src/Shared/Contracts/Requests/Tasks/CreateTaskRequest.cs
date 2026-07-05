namespace Contracts.Requests.Tasks;

public record CreateTaskRequest(
    string Title,
    string? Description,
    DateTimeOffset? DueDate,
    Guid? AssigneeId);
