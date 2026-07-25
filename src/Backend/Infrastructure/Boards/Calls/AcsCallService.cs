using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Azure;
using Azure.Communication;
using Azure.Communication.Identity;
using Azure.Communication.Rooms;
using Azure.Core;
using Contracts.DTOs;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Boards.Calls;

public class AcsCallService(
    CommunicationIdentityClient identityClient,
    RoomsClient roomsClient,
    IUserRepository userRepository,
    ILogger<AcsCallService> logger) : IAcsCallService
{
    public async Task<string> EnsureUserIdentityAsync(Guid userId, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);

        var user = await userRepository.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User not found.");

        if (!string.IsNullOrWhiteSpace(user.AcsCommunicationUserId))
        {
            return user.AcsCommunicationUserId;
        }

        var identity = await identityClient.CreateUserAsync(ct);
        user.AcsCommunicationUserId = identity.Value.Id;

        // Mutates the tracked entity only — the caller's own SaveChangesAsync commits this,
        // matching the "one handler, one transaction" pattern used everywhere else in this codebase.
        userRepository.Update(user);

        logger.LogInformation("Provisioned ACS identity for user {UserId}", userId);

        return user.AcsCommunicationUserId;
    }

    public async Task<string> CreateRoomAsync(CancellationToken ct = default)
    {
        try
        {
            var room = await roomsClient.CreateRoomAsync(
                validFrom: null,
                validUntil: null,
                participants: [],
                cancellationToken: ct);

            logger.LogInformation("Created ACS room {RoomId}", room.Value.Id);

            return room.Value.Id;
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex, "Failed to create ACS room");
            throw;
        }
    }

    public async Task AddOrUpdateParticipantAsync(string roomId, string acsUserId, CallParticipantRole role, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);
        ArgumentException.ThrowIfNullOrWhiteSpace(acsUserId);

        var participant = new RoomParticipant(new CommunicationUserIdentifier(acsUserId))
        {
            Role = ToAcsRole(role)
        };

        try
        {
            await roomsClient.AddOrUpdateParticipantsAsync(roomId, [participant], ct);

            logger.LogInformation("Added/updated participant {AcsUserId} in ACS room {RoomId} as {Role}", acsUserId, roomId, role);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            logger.LogWarning(ex, "ACS room {RoomId} not found while adding participant {AcsUserId}", roomId, acsUserId);
            throw new NotFoundException("The call room no longer exists.");
        }
    }

    public async Task RemoveParticipantAsync(string roomId, string acsUserId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);
        ArgumentException.ThrowIfNullOrWhiteSpace(acsUserId);

        try
        {
            await roomsClient.RemoveParticipantsAsync(roomId, [new CommunicationUserIdentifier(acsUserId)], ct);

            logger.LogInformation("Removed participant {AcsUserId} from ACS room {RoomId}", acsUserId, roomId);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Idempotent: the room or participant is already gone, which is exactly the caller's intent.
            logger.LogDebug(ex, "ACS room {RoomId} or participant {AcsUserId} already gone; treating remove as a no-op", roomId, acsUserId);
        }
    }

    public async Task DeleteRoomAsync(string roomId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);

        try
        {
            await roomsClient.DeleteRoomAsync(roomId, ct);

            logger.LogInformation("Deleted ACS room {RoomId}", roomId);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Idempotent: already deleted (e.g. a retried "end call") is a successful outcome, not a failure.
            logger.LogDebug(ex, "ACS room {RoomId} already deleted; treating delete as a no-op", roomId);
        }
    }

    public async Task<AcsCallCredentialsDto> IssueTokenAsync(string acsUserId, string roomId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(acsUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);

        Response<AccessToken> token;

        try
        {
            token = await identityClient.GetTokenAsync(
                new CommunicationUserIdentifier(acsUserId),
                [CommunicationTokenScope.VoIP],
                ct);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            logger.LogWarning(ex, "ACS identity {AcsUserId} not found while issuing token", acsUserId);
            throw new NotFoundException("The calling identity no longer exists.");
        }

        logger.LogInformation("Issued ACS token for {AcsUserId} scoped to room {RoomId}", acsUserId, roomId);

        return new AcsCallCredentialsDto(token.Value.Token, token.Value.ExpiresOn, acsUserId, roomId);
    }

    private static ParticipantRole ToAcsRole(CallParticipantRole role) => role switch
    {
        CallParticipantRole.Presenter => ParticipantRole.Presenter,
        CallParticipantRole.Attendee => ParticipantRole.Attendee,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown call participant role.")
    };
}
