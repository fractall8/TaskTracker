using Contracts.DTOs;

namespace Services.Abstractions.FaqChat;

public interface IFaqChatApiService
{
    Task<FaqAnswerDto> AskAsync(
        string question,
        IReadOnlyList<FaqChatTurnDto> history,
        CancellationToken ct = default);
}
