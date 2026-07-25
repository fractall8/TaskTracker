namespace Contracts.DTOs;

public record BoardCallParticipantDto(
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    DateTimeOffset JoinedAt);
