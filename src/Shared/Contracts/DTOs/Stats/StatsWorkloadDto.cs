namespace Contracts.DTOs;

// UserId is null for the unassigned bucket, which is a real row rather than an omission: open work nobody
// owns is the most actionable thing this panel shows. Open tasks as of now (EPIC 5 Decision 4).
public record StatsWorkloadDto(Guid? UserId, string Name, string? AvatarUrl, int OnTrack, int Overdue);
