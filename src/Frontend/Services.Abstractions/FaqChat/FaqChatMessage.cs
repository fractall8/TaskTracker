using Contracts.DTOs;
using Contracts.Enums;

namespace Services.Abstractions.FaqChat;

/// <summary>
/// A rendered conversation entry. Wraps <see cref="FaqChatTurnDto"/> with the display-only state the
/// wire contract does not carry — citations and whether the answer was grounded.
/// </summary>
public record FaqChatMessage(
    FaqChatRoleDto Role,
    string Content,
    bool IsGrounded = true,
    IReadOnlyList<FaqCitationDto>? Citations = null)
{
    public FaqChatTurnDto ToTurn() => new(Role, Content);
}
