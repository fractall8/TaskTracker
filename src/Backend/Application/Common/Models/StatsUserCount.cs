namespace Application.Common.Models;

// Reported and completed are keyed on different columns, so each is fetched separately and merged by user.
public record StatsUserCount(Guid UserId, string Name, string? AvatarUrl, int Count);
