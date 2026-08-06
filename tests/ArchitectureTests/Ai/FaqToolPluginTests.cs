using Infrastructure.Ai.Tools;
using MediatR;
using Microsoft.SemanticKernel;

namespace ArchitectureTests.Ai;

public class FaqToolPluginTests
{
    private static readonly string[] _expectedFunctions =
    [
        "list_my_workspaces",
        "get_workspace_overview",
        "list_boards",
        "get_board_summary",
        "list_tasks",
        "count_workspace_tasks",
        "list_workspace_overdue_tasks",
        "list_workspace_tasks_due_soon",
        "get_my_plan_limits"
    ];

    private static KernelPlugin Plugin() =>
        KernelPluginFactory.CreateFromObject(new FaqToolPlugin(new UnusedSender()), FaqToolPlugin.PluginName);

    [Fact]
    public void Plugin_exposes_exactly_the_whitelisted_tools()
    {
        var names = Plugin().Select(function => function.Name).OrderBy(name => name, StringComparer.Ordinal);

        Assert.Equal(_expectedFunctions.OrderBy(name => name, StringComparer.Ordinal), names);
    }

    [Fact]
    public void Every_tool_has_a_description_the_model_can_route_on()
    {
        var undescribed = Plugin()
            .Where(function => (function.Description?.Length ?? 0) < 40)
            .Select(function => function.Name)
            .ToList();

        Assert.True(undescribed.Count == 0, $"Missing or too-short description: {string.Join(", ", undescribed)}");
    }

    [Fact]
    public void Every_parameter_is_described_and_none_identifies_a_user()
    {
        var problems = (
            from function in Plugin()
            from parameter in function.Metadata.Parameters
            where string.IsNullOrWhiteSpace(parameter.Description)
                  || parameter.Name.Contains("user", StringComparison.OrdinalIgnoreCase)
                  || parameter.Name.Contains("assignee", StringComparison.OrdinalIgnoreCase)
            select $"{function.Name}.{parameter.Name}").ToList();

        Assert.True(
            problems.Count == 0,
            $"Undescribed or identity-bearing parameter: {string.Join(", ", problems)}");
    }

    // The plugin's function list and the handler catalogue are declared separately, so they can drift:
    // a handler added without a KernelFunction is unreachable, and the reverse fails at run time.
    [Fact]
    public void Plugin_exposes_one_function_per_tool_handler()
    {
        var handlers = AiToolRules.ToolTypes(typeof(Application.Interfaces.Services.IFaqAssistantService).Assembly);

        Assert.Equal(handlers.Count, Plugin().Count());
    }

    [Fact]
    public void No_tool_name_promises_open_tasks()
    {
        // TaskItem has no completion state, so a tool named *open* would be lying to the model.
        Assert.DoesNotContain(Plugin(), function => function.Name.Contains("open", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class UnusedSender : ISender
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) =>
            throw new InvalidOperationException("Sender reached during metadata inspection.");

        public Task Send<TRequest>(TRequest request, CancellationToken ct = default)
            where TRequest : IRequest =>
            throw new InvalidOperationException("Sender reached during metadata inspection.");

        public Task<object?> Send(object request, CancellationToken ct = default) =>
            throw new InvalidOperationException("Sender reached during metadata inspection.");

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request, CancellationToken ct = default) =>
            throw new InvalidOperationException("Sender reached during metadata inspection.");

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken ct = default) =>
            throw new InvalidOperationException("Sender reached during metadata inspection.");
    }
}
