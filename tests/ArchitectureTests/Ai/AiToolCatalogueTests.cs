using System.Reflection;
using Application.Features.Ai.Tools;
using Application.Interfaces.Services;
using MediatR;

namespace ArchitectureTests.Ai;

public class AiToolCatalogueTests
{
    private static readonly Assembly _applicationAssembly = typeof(IFaqAssistantService).Assembly;

    // Adding a tool is a deliberate act: this list must be updated alongside the handler, and the epic's
    // tool catalogue reviewed.
    private static readonly Type[] _expectedTools =
    [
        typeof(ListMyWorkspacesTool),
        typeof(GetWorkspaceOverviewTool),
        typeof(ListBoardsTool),
        typeof(GetBoardSummaryTool),
        typeof(ListTasksTool),
        typeof(CountWorkspaceTasksTool),
        typeof(ListWorkspaceOverdueTasksTool),
        typeof(ListWorkspaceTasksDueSoonTool),
        typeof(GetMyPlanLimitsTool)
    ];

    [Fact]
    public void Catalogue_contains_exactly_the_expected_eight_tools()
    {
        var handlers = AiToolRules.ToolTypes(_applicationAssembly);

        Assert.Equal(_expectedTools.Length, handlers.Count);
    }

    [Fact]
    public void Every_expected_tool_has_a_handler()
    {
        var handled = AiToolRules.ToolTypes(_applicationAssembly)
            .SelectMany(handler => handler.GetInterfaces())
            .Where(contract => contract.Name.StartsWith("IRequestHandler", StringComparison.Ordinal))
            .Select(contract => contract.GetGenericArguments()[0])
            .ToList();

        Assert.All(_expectedTools, tool => Assert.Contains(tool, handled));
    }

    [Theory]
    [MemberData(nameof(Tools))]
    public void Tool_returns_only_approved_projections(Type tool)
    {
        var resultType = tool.GetInterfaces()
            .Single(contract => contract.IsGenericType
                               && contract.GetGenericTypeDefinition() == typeof(IRequest<>))
            .GetGenericArguments()[0];

        var leafType = resultType.IsGenericType && resultType.GetGenericArguments().Length == 1
            ? resultType.GetGenericArguments()[0]
            : resultType;

        Assert.Equal(AiProjectionRules.ProjectionNamespace, leafType.Namespace);
    }

    [Theory]
    [MemberData(nameof(Tools))]
    public void Tool_takes_no_user_or_identity_argument(Type tool)
    {
        var forbidden = tool
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.Name.Contains("User", StringComparison.OrdinalIgnoreCase)
                               || property.Name.Contains("Member", StringComparison.OrdinalIgnoreCase)
                               || property.Name.Contains("Assignee", StringComparison.OrdinalIgnoreCase))
            .Select(property => property.Name)
            .ToList();

        Assert.True(
            forbidden.Count == 0,
            $"{tool.Name} accepts {string.Join(", ", forbidden)}. Identity is injected from the "
            + "authenticated principal, never supplied by the model (EPIC 3 Decision 4).");
    }

    [Fact]
    public void Every_workspace_scoped_tool_requires_a_workspace_or_board_id()
    {
        var unscoped = _expectedTools
            .Where(tool => tool != typeof(ListMyWorkspacesTool))
            .Where(tool => !tool.GetProperties().Any(property =>
                property.Name is "WorkspaceId" or "BoardId" && property.PropertyType == typeof(Guid)))
            .Select(tool => tool.Name)
            .ToList();

        Assert.True(
            unscoped.Count == 0,
            $"{string.Join(", ", unscoped)} is not scoped to a single workspace or board "
            + "(EPIC 3 Decision 15). Only ListMyWorkspacesTool may be unscoped.");
    }

    public static TheoryData<Type> Tools
    {
        get
        {
            var data = new TheoryData<Type>();

            foreach (var tool in _expectedTools)
            {
                data.Add(tool);
            }

            return data;
        }
    }
}
