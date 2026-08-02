using Contracts.DTOs;

namespace Contracts.Requests.FaqChat;

public record AskFaqQuestionRequest(string Question, IReadOnlyList<FaqChatTurnDto> History);
