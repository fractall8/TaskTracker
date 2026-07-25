using Application.Common.Enums;
using Contracts.DTOs;

namespace Application.Interfaces.Services;

public interface IAcsCallService
{
    Task<string> EnsureUserIdentityAsync(Guid userId, CancellationToken ct = default);

    Task<string> CreateRoomAsync(CancellationToken ct = default);

    Task AddOrUpdateParticipantAsync(string roomId, string acsUserId, CallParticipantRole role, CancellationToken ct = default);

    Task RemoveParticipantAsync(string roomId, string acsUserId, CancellationToken ct = default);

    Task DeleteRoomAsync(string roomId, CancellationToken ct = default);

    Task<AcsCallCredentialsDto> IssueTokenAsync(string acsUserId, string roomId, CancellationToken ct = default);
}
