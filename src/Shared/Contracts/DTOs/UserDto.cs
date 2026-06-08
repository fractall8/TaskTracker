namespace Contracts.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string? DisplayName);