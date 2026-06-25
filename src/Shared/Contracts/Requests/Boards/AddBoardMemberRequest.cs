using Contracts.Enums;

namespace Contracts.Requests.Boards;

public record AddBoardMemberRequest(
    Guid WorkspaceMemberId,
    BoardRoleDto Role);
