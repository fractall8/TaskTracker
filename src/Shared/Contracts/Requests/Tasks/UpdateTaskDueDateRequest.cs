namespace Contracts.Requests.Tasks;

public record UpdateTaskDueDateRequest(DateTimeOffset? DueDate);
