using System.ComponentModel;
using Application.Ai.Projections;
using Application.Features.Ai.Tools;
using MediatR;
using Microsoft.SemanticKernel;

namespace Infrastructure.Ai.Tools;

// The whitelist the model sees. Descriptions are the routing signal, so they must describe exactly what
// each tool returns and promise nothing the schema cannot deliver. No parameter identifies a user:
// identity is resolved inside each handler from the authenticated principal.
public class FaqToolPlugin(ISender sender)
{
    public const string PluginName = "TaskTrackerData";

    private const string _workspaceIdHint =
        "The workspace id. If it is not already known from earlier in this conversation, "
        + "call list_my_workspaces first to find it.";

    private const string _boardIdHint =
        "The board id. If it is not already known from earlier in this conversation, "
        + "call list_boards first to find it.";

    [KernelFunction("list_my_workspaces")]
    [Description("Lists the workspaces the current user belongs to, with their role in each and how many "
                 + "boards they can see. Use this to discover workspace ids when they are not already known.")]
    public Task<IReadOnlyList<AiWorkspaceSummary>> ListMyWorkspacesAsync(CancellationToken ct) =>
        sender.Send(new ListMyWorkspacesTool(), ct);

    [KernelFunction("get_workspace_overview")]
    [Description("Gets one workspace's name, the user's own role in it, and how many boards and members it "
                 + "has. Use for questions about a workspace's size or what role the user holds there.")]
    public Task<AiWorkspaceUsage> GetWorkspaceOverviewAsync(
        [Description(_workspaceIdHint)] Guid workspaceId,
        CancellationToken ct) =>
        sender.Send(new GetWorkspaceOverviewTool(workspaceId), ct);

    [KernelFunction("list_boards")]
    [Description("Lists the boards in one workspace that the user can see, with each board's column and "
                 + "task counts, whether it is archived, and the user's own role on it.")]
    public Task<IReadOnlyList<AiBoardSummary>> ListBoardsAsync(
        [Description(_workspaceIdHint)] Guid workspaceId,
        [Description("Include archived boards. Defaults to false.")] bool includeArchived,
        CancellationToken ct) =>
        sender.Send(new ListBoardsTool(workspaceId, includeArchived), ct);

    [KernelFunction("get_board_summary")]
    [Description("Gets one board's task counts broken down by column, plus how many of its tasks are "
                 + "overdue and how many are unassigned. Use when asked about a specific board's contents.")]
    public Task<AiBoardDetail> GetBoardSummaryAsync(
        [Description(_boardIdHint)] Guid boardId,
        CancellationToken ct) =>
        sender.Send(new GetBoardSummaryTool(boardId), ct);

    [KernelFunction("list_tasks")]
    [Description("Lists individual tasks on one board with their title, column, due date and whether they "
                 + "are assigned to the current user. Does not return task descriptions, comments, or who a "
                 + "task is assigned to.")]
    public Task<IReadOnlyList<AiTaskSummary>> ListTasksAsync(
        [Description(_boardIdHint)] Guid boardId,
        [Description("Restrict to one column. Omit for all columns.")] Guid? columnId,
        [Description("Only tasks assigned to the current user.")] bool onlyAssignedToMe,
        [Description("Only tasks whose due date has already passed.")] bool onlyOverdue,
        [Description("How many tasks to return. Omit for the default.")] int? take,
        CancellationToken ct) =>
        sender.Send(new ListTasksTool(boardId, columnId, onlyAssignedToMe, onlyOverdue, DueBefore: null, take), ct);

    [KernelFunction("count_workspace_tasks")]
    [Description("Returns task counts for one workspace: the total, how many are overdue, how many are due "
                 + "within seven days, and how many are assigned to the user. Use when the question asks "
                 + "how many. Tasks have no completed state, so this counts all tasks, not only open ones.")]
    public Task<AiTaskCounts> CountWorkspaceTasksAsync(
        [Description(_workspaceIdHint)] Guid workspaceId,
        [Description("Restrict to one board. Omit to count the whole workspace.")] Guid? boardId,
        CancellationToken ct) =>
        sender.Send(new CountWorkspaceTasksTool(workspaceId, boardId), ct);

    [KernelFunction("list_workspace_overdue_tasks")]
    [Description("Lists the individual overdue tasks across every board the user can see in one workspace, "
                 + "soonest due date first. Use when the question asks which tasks are overdue rather than "
                 + "how many.")]
    public Task<IReadOnlyList<AiTaskSummary>> ListWorkspaceOverdueTasksAsync(
        [Description(_workspaceIdHint)] Guid workspaceId,
        [Description("How many tasks to return. Omit for the default.")] int? take,
        CancellationToken ct) =>
        sender.Send(new ListWorkspaceOverdueTasksTool(workspaceId, take), ct);

    [KernelFunction("get_my_plan_limits")]
    [Description("Gets one workspace's subscription plan, every limit that plan imposes, current board and "
                 + "member usage, and whether board export is included. Use for questions about plans, "
                 + "limits, quotas, or whether a paid feature is available.")]
    public Task<AiPlanLimits> GetMyPlanLimitsAsync(
        [Description(_workspaceIdHint)] Guid workspaceId,
        CancellationToken ct) =>
        sender.Send(new GetMyPlanLimitsTool(workspaceId), ct);
}
