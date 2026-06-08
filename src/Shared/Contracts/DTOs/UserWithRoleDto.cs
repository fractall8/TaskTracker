using Contracts.Enums;

namespace Contracts.DTOs;

public record UserWithRoleDto(
    Guid Id,
    string Email,
    string? DisplayName,
    BoardRoleDto Role);