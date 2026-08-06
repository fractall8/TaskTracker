using Application.Features.FaqChat.Commands;
using Application.Options;
using Contracts.DTOs;
using Contracts.Enums;
using Microsoft.Extensions.Options;

namespace ArchitectureTests.Ai;

public class AskFaqQuestionValidatorTests
{
    private static readonly FaqChatOptions _options = new();

    private static AskFaqQuestionCommandValidator Validator() =>
        new(Options.Create(_options));

    private static AskFaqQuestionCommand Command(string question, params FaqChatTurnDto[] history) =>
        new(question, history);

    private static FaqChatTurnDto Assistant(int length) =>
        new(FaqChatRoleDto.Assistant, new string('a', length));

    // The assistant's own answers are replayed as history and are bounded by MaxOutputTokens, not by what
    // a user can type. Capping them at MaxQuestionLength bricked conversations after any long answer.
    [Fact]
    public void An_assistant_answer_longer_than_the_question_limit_is_accepted_in_history()
    {
        var result = Validator().Validate(Command("next question", Assistant(_options.MaxQuestionLength + 500)));

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void History_turns_are_still_bounded()
    {
        var result = Validator().Validate(Command("q", Assistant(_options.MaxHistoryTurnLength + 1)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void The_question_itself_keeps_the_tighter_limit()
    {
        Assert.False(Validator().Validate(Command(new string('a', _options.MaxQuestionLength + 1))).IsValid);
        Assert.True(Validator().Validate(Command(new string('a', _options.MaxQuestionLength))).IsValid);
    }

    [Fact]
    public void The_history_turn_limit_leaves_room_for_a_full_length_answer()
    {
        // AzureOpenAi:MaxOutputTokens is 4096; at roughly four characters per token an answer can approach
        // 16k characters. If MaxOutputTokens is raised, this is the guard that must move with it.
        const int worstCaseAnswerLength = 4096 * 4;

        Assert.True(
            _options.MaxHistoryTurnLength >= worstCaseAnswerLength,
            $"MaxHistoryTurnLength ({_options.MaxHistoryTurnLength}) must exceed the longest answer the "
            + $"model can produce (~{worstCaseAnswerLength} characters), or long answers will break the "
            + "next request in the conversation.");
    }

    [Fact]
    public void Too_many_turns_are_rejected()
    {
        var history = Enumerable.Range(0, _options.MaxHistoryTurns + 1).Select(_ => Assistant(10)).ToArray();

        Assert.False(Validator().Validate(Command("q", history)).IsValid);
    }

    [Fact]
    public void An_empty_turn_is_rejected()
    {
        var result = Validator().Validate(Command("q", new FaqChatTurnDto(FaqChatRoleDto.User, "")));

        Assert.False(result.IsValid);
    }
}
