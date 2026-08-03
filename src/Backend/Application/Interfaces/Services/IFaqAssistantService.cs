using Contracts.DTOs;

namespace Application.Interfaces.Services;

public interface IFaqAssistantService
{
    Task<FaqAnswerDto> AskAsync(
        string question,
        IReadOnlyList<FaqChatTurnDto> history,
        CancellationToken ct = default);
}
