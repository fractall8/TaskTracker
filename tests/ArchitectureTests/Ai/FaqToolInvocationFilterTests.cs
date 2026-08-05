using Domain.Exceptions;
using Infrastructure.Ai.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;

namespace ArchitectureTests.Ai;

// Driven through a real Kernel invocation rather than a hand-built context, so the filter is exercised on
// the same path Semantic Kernel uses in production.
public class FaqToolInvocationFilterTests
{
    [Fact]
    public async Task Calls_within_budget_are_invoked()
    {
        var budget = new AiToolBudget(2);
        var invoked = 0;
        var kernel = KernelWith(budget, () => { invoked++; return "ok"; });

        await kernel.InvokeAsync("probe", "probe");
        await kernel.InvokeAsync("probe", "probe");

        Assert.Equal(2, invoked);
        Assert.Equal(2, budget.Used);
    }

    [Fact]
    public async Task Call_beyond_budget_is_not_invoked_and_returns_a_terminal_message()
    {
        var invoked = 0;
        var kernel = KernelWith(new AiToolBudget(1), () => { invoked++; return "ok"; });

        await kernel.InvokeAsync("probe", "probe");
        var blocked = await kernel.InvokeAsync("probe", "probe");

        Assert.Equal(1, invoked);
        Assert.Equal(FaqToolInvocationFilter.BudgetExhaustedResult, blocked.GetValue<string>());
    }

    [Fact]
    public async Task Authorization_refusal_becomes_a_relayable_result_rather_than_aborting_the_turn()
    {
        var kernel = KernelWith(
            new AiToolBudget(5),
            () => throw new ForbiddenException("You are not a member of this workspace."));

        var result = await kernel.InvokeAsync("probe", "probe");

        Assert.Equal("You are not a member of this workspace.", result.GetValue<string>());
    }

    [Fact]
    public async Task Unexpected_failure_is_shaped_and_does_not_leak_the_exception()
    {
        var kernel = KernelWith(
            new AiToolBudget(5),
            () => throw new InvalidOperationException("connection string=secret;"));

        var result = (await kernel.InvokeAsync("probe", "probe")).GetValue<string>();

        Assert.DoesNotContain("secret", result!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("That information could not be retrieved right now.", result);
    }

    [Fact]
    public async Task Bad_arguments_get_a_corrective_message_the_model_can_act_on()
    {
        var kernel = KernelWith(
            new AiToolBudget(5),
            () => throw new KernelException(
                "Missing argument for function parameter 'workspaceId'",
                new ArgumentException("Unrecognized Guid format.", "workspaceId")));

        var result = (await kernel.InvokeAsync("probe", "probe")).GetValue<string>();

        Assert.Contains("GUID", result!, StringComparison.Ordinal);
        Assert.Contains("call the tool again", result!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cancellation_is_not_swallowed()
    {
        var kernel = KernelWith(new AiToolBudget(5), () => throw new OperationCanceledException());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => kernel.InvokeAsync("probe", "probe"));
    }

    private static Kernel KernelWith(AiToolBudget budget, Func<string> body)
    {
        var kernel = Kernel.CreateBuilder().Build();

        kernel.FunctionInvocationFilters.Add(
            new FaqToolInvocationFilter(budget, NullLogger<FaqToolInvocationFilter>.Instance));

        kernel.Plugins.AddFromFunctions("probe", [KernelFunctionFactory.CreateFromMethod(body, "probe")]);

        return kernel;
    }
}
