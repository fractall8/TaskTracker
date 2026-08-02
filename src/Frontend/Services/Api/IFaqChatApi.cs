using Contracts.DTOs;
using Contracts.Requests.FaqChat;
using Refit;

namespace Services.Api;

public interface IFaqChatApi
{
    [Post("/api/faq-chat/ask")]
    Task<IApiResponse<FaqAnswerDto>> AskAsync(
        [Body] AskFaqQuestionRequest request,
        CancellationToken ct = default);
}
