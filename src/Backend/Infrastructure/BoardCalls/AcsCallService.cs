using Application.Common.Enums;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Azure.Communication;
using Azure.Communication.Identity;
using Azure.Communication.Rooms;
using Contracts.DTOs;
using Domain.Exceptions;

namespace Infrastructure.BoardCalls;

public class AcsCallService(
    CommunicationIdentityClient identityClient,
    RoomsClient roomsClient,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IAcsCallService
{
    public async Task<string> EnsureUserIdentityAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User not found.");

        if (!string.IsNullOrWhiteSpace(user.AcsCommunicationUserId))
        {
            return user.AcsCommunicationUserId;
        }

        var identity = await identityClient.CreateUserAsync(ct);
        user.AcsCommunicationUserId = identity.Value.Id;

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);

        return user.AcsCommunicationUserId;
    }

    public async Task<string> CreateRoomAsync(CancellationToken ct = default)
    {
        var room = await roomsClient.CreateRoomAsync(
            validFrom: null,
            validUntil: null,
            participants: [],
            cancellationToken: ct);

        return room.Value.Id;
    }

    public async Task AddOrUpdateParticipantAsync(string roomId, string acsUserId, CallParticipantRole role, CancellationToken ct = default)
    {
        var participant = new RoomParticipant(new CommunicationUserIdentifier(acsUserId))
        {
            Role = ToAcsRole(role)
        };

        await roomsClient.AddOrUpdateParticipantsAsync(roomId, [participant], ct);
    }

    public async Task RemoveParticipantAsync(string roomId, string acsUserId, CancellationToken ct = default)
    {
        await roomsClient.RemoveParticipantsAsync(roomId, [new CommunicationUserIdentifier(acsUserId)], ct);
    }

    public async Task DeleteRoomAsync(string roomId, CancellationToken ct = default)
    {
        await roomsClient.DeleteRoomAsync(roomId, ct);
    }

    public async Task<AcsCallCredentialsDto> IssueTokenAsync(string acsUserId, string roomId, CancellationToken ct = default)
    {
        var token = await identityClient.GetTokenAsync(
            new CommunicationUserIdentifier(acsUserId),
            [CommunicationTokenScope.VoIP],
            ct);

        return new AcsCallCredentialsDto(token.Value.Token, token.Value.ExpiresOn, acsUserId, roomId);
    }

    private static ParticipantRole ToAcsRole(CallParticipantRole role) => role switch
    {
        CallParticipantRole.Presenter => ParticipantRole.Presenter,
        CallParticipantRole.Attendee => ParticipantRole.Attendee,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown call participant role.")
    };
}
