namespace Contracts.DTOs;

public record BoardCallParticipantDto(
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    string? AcsCommunicationUserId,
    DateTimeOffset JoinedAt);
