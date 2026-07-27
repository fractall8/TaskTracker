namespace Contracts.DTOs;

public record AcsCallCredentialsDto(
    string Token,
    DateTimeOffset ExpiresOn,
    string AcsUserId,
    string RoomId);
