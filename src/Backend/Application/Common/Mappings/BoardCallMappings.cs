using Contracts.DTOs;
using Domain.Entities;

namespace Application.Common.Mappings;

internal static class BoardCallMappings
{
    public static BoardCallDto ToDto(BoardCall call) =>
        new(
            call.Id,
            call.BoardId,
            call.StartedByUserId,
            call.StartedAt,
            call.Participants
                .Where(p => p.LeftAt == null)
                .Select(p => new BoardCallParticipantDto(p.UserId, p.User?.DisplayName, p.User?.AvatarUrl, p.JoinedAt))
                .ToList());
}
