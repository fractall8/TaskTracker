using Contracts.DTOs;
using Domain.Entities;

namespace Application.Common.Mappings;

internal static class BoardCallMappings
{
    public static BoardCallDto ToDto(BoardCall call, int maxParticipants) =>
        new(
            call.Id,
            call.BoardId,
            call.StartedByUserId,
            call.StartedAt,
            maxParticipants,
            ToParticipantDtos(call));

    public static List<BoardCallParticipantDto> ToParticipantDtos(BoardCall call) =>
        call.Participants
            .Where(p => p.LeftAt == null)
            .Select(p => new BoardCallParticipantDto(p.UserId, p.User?.DisplayName, p.User?.AvatarUrl, p.User?.AcsCommunicationUserId, p.JoinedAt))
            .ToList();
}
