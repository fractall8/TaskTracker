using Contracts.Enums;

namespace Contracts.Requests.Boards;

public record UpdateBoardMemberRoleRequest(
    BoardRoleDto Role);
