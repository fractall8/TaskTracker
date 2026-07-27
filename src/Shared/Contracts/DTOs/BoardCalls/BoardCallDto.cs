namespace Contracts.DTOs;

public record BoardCallDto(
    Guid Id,
    Guid BoardId,
    Guid StartedByUserId,
    DateTimeOffset StartedAt,
    int MaxParticipants,
    IReadOnlyList<BoardCallParticipantDto> Participants);
