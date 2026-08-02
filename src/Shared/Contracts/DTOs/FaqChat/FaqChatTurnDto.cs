using Contracts.Enums;

namespace Contracts.DTOs;

public record FaqChatTurnDto(FaqChatRoleDto Role, string Content);
