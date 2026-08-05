using Application.Behaviors;
using Application.Interfaces.Services;
using Application.Options;
using Contracts.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.FaqChat.Commands;

public record AskFaqQuestionCommand(string Question, IReadOnlyList<FaqChatTurnDto> History)
    : IRequest<FaqAnswerDto>, ISensitivePayload;

public class AskFaqQuestionCommandHandler(IFaqAssistantService faqAssistantService)
    : IRequestHandler<AskFaqQuestionCommand, FaqAnswerDto>
{
    public async Task<FaqAnswerDto> Handle(AskFaqQuestionCommand request, CancellationToken ct)
    {
        return await faqAssistantService.AskAsync(request.Question, request.History, ct);
    }
}

public class AskFaqQuestionCommandValidator : AbstractValidator<AskFaqQuestionCommand>
{
    public AskFaqQuestionCommandValidator(IOptions<FaqChatOptions> options)
    {
        var faqChatOptions = options.Value;

        RuleFor(x => x.Question)
            .NotEmpty().WithMessage("Question is required.")
            .MaximumLength(faqChatOptions.MaxQuestionLength);

        RuleFor(x => x.History)
            .NotNull().WithMessage("History is required.")
            .DependentRules(() =>
            {
                RuleFor(x => x.History)
                    .Must(history => history.Count <= faqChatOptions.MaxHistoryTurns)
                    .WithMessage($"History must not contain more than {faqChatOptions.MaxHistoryTurns} turns.");

                RuleForEach(x => x.History).ChildRules(turn =>
                {
                    turn.RuleFor(x => x.Role).IsInEnum();

                    turn.RuleFor(x => x.Content)
                        .NotEmpty().WithMessage("History turn content must not be empty.")
                        .MaximumLength(faqChatOptions.MaxQuestionLength);
                });
            });
    }
}
