namespace Contracts.Notifications.BoardActions.Payloads;

public record TaskDueDateUpdatedPayload(
    Guid TaskId,
    DateTimeOffset? DueDate) : BoardActionPayload;
