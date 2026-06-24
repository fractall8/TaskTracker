namespace Contracts.DTOs;

public record UserProfileDto(
    Guid Id,
    string Email,
    string? DisplayName,
    string? AvatarUrl);
