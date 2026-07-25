namespace Contracts.DTOs;

public record BoardCallDto(
    Guid Id,
    Guid BoardId,
    Guid StartedByUserId,
    DateTimeOffset StartedAt,
    IReadOnlyList<BoardCallParticipantDto> Participants);
