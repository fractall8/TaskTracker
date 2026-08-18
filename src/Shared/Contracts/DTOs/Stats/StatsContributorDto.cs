namespace Contracts.DTOs;

// Reported counts tasks the user raised in the period; Completed counts tasks they closed in it. The two are
// separate populations and must never be divided by one another (EPIC 5 Decision 5) — the client renders
// them as two magnitudes on a shared scale.
public record StatsContributorDto(Guid UserId, string Name, string? AvatarUrl, int Reported, int Completed);
