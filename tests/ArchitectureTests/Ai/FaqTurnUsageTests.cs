using System.Reflection;
using Infrastructure.Ai.Tools;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ArchitectureTests.Ai;

// FaqTurnUsage is internal, so it is exercised by reflection rather than by widening its visibility for a
// test. What matters is that a turn's cost is summed across every round trip, not just the final one.
public class FaqTurnUsageTests
{
    private static readonly Type _usageType = typeof(FaqToolPlugin).Assembly
        .GetType("Infrastructure.Ai.FaqTurnUsage", throwOnError: true)!;

    [Fact]
    public void A_turn_with_no_usage_metadata_reports_zero()
    {
        var history = new ChatHistory();
        history.AddUserMessage("question");

        var usage = Measure(history, null);

        Assert.Equal(0, Property(usage, "ModelCalls"));
        Assert.Equal(0, Property(usage, "InputTokens"));
    }

    [Fact]
    public void The_final_message_is_not_counted_twice_when_it_is_already_in_the_history()
    {
        var history = new ChatHistory();
        var reply = WithUsage(120, 30, 10);
        history.Add(reply);

        var usage = Measure(history, reply);

        Assert.Equal(1, Property(usage, "ModelCalls"));
        Assert.Equal(120, Property(usage, "InputTokens"));
        Assert.Equal(30, Property(usage, "OutputTokens"));
        Assert.Equal(10, Property(usage, "ReasoningTokens"));
    }

    [Fact]
    public void Every_round_trip_of_an_auto_invoked_turn_is_counted()
    {
        // The shape auto-invocation produces: one assistant message per round trip, then a final answer.
        var history = new ChatHistory();
        history.AddSystemMessage("system");
        history.AddUserMessage("question");
        history.Add(WithUsage(100, 20, 8));
        history.Add(WithUsage(200, 40, 16));

        var usage = Measure(history, WithUsage(300, 60, 24));

        Assert.Equal(3, Property(usage, "ModelCalls"));
        Assert.Equal(600, Property(usage, "InputTokens"));
        Assert.Equal(120, Property(usage, "OutputTokens"));
        Assert.Equal(48, Property(usage, "ReasoningTokens"));
    }

    private static ChatMessageContent WithUsage(int input, int output, int reasoning)
    {
        var usage = OpenAI.Chat.OpenAIChatModelFactory.ChatTokenUsage(
            outputTokenCount: output,
            inputTokenCount: input,
            totalTokenCount: input + output,
            outputTokenDetails: OpenAI.Chat.OpenAIChatModelFactory.ChatOutputTokenUsageDetails(
                reasoningTokenCount: reasoning));

        return new ChatMessageContent(AuthorRole.Assistant, "reply")
        {
            Metadata = new Dictionary<string, object?> { ["Usage"] = usage }
        };
    }

    private static object Measure(ChatHistory history, ChatMessageContent? final) =>
        _usageType.GetMethod("Measure", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [history, final])!;

    private static int Property(object usage, string name) =>
        (int)_usageType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!.GetValue(usage)!;
}
