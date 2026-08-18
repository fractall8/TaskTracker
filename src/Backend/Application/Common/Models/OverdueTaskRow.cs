namespace Application.Common.Models;

// Days overdue cannot be computed in SQL with calendar semantics, so the raw due date comes back and the
// handler turns it into a day count in the caller's offset.
public record OverdueTaskRow(
    Guid TaskId,
    string Title,
    Guid BoardId,
    string BoardName,
    string? AssigneeName,
    string? AssigneeAvatarUrl,
    DateTimeOffset DueDate);
