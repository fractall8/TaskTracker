using Contracts.Enums;

namespace Contracts.DTOs;

public record AddBoardMemberRequest(
    Guid WorkspaceMemberId,
    BoardRoleDto Role);
