using Contracts.DTOs;
using Contracts.Requests.FaqChat;
using Services.Abstractions.FaqChat;
using Services.Api;
using Services.Extensions;

namespace Services.FaqChat;

public class FaqChatApiService(IFaqChatApi faqChatApi) : IFaqChatApiService
{
    public async Task<FaqAnswerDto> AskAsync(
        string question,
        IReadOnlyList<FaqChatTurnDto> history,
        CancellationToken ct = default)
    {
        var response = await faqChatApi.AskAsync(new AskFaqQuestionRequest(question, history), ct);

        return await response.HandleResponseAsync();
    }
}
