namespace Contracts.DTOs;

// AssigneeName is null for unassigned work, which is exactly the kind of overdue task worth surfacing.
// DaysOverdue counts whole calendar days in the caller's offset, so it is never zero.
public record StatsOverdueTaskDto(
    Guid TaskId,
    string Title,
    Guid BoardId,
    string BoardName,
    string? AssigneeName,
    string? AssigneeAvatarUrl,
    DateTimeOffset DueDate,
    int DaysOverdue);

// Total is the real count, which can exceed Tasks.Count because the list is capped (EPIC 5 Decision 9).
// The badge must show Total, never Tasks.Count, or a truncated list understates the problem.
public record StatsOverdueDto(int Total, List<StatsOverdueTaskDto> Tasks);
