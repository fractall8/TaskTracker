using Application.Ai.Projections;
using Application.Common.Models;
using Application.Features.Ai.Tools;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Options;
using Contracts.DTOs;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace ArchitectureTests.Ai;

// Proves each handler authorizes before it reads. The access services are faked to refuse, and the
// repository throws if reached — so a handler that forgot its Ensure*Async call fails loudly.
public class AiToolAuthorizationTests
{
    public static TheoryData<string> Tools =>
    [
        nameof(ListMyWorkspacesTool),
        nameof(GetWorkspaceOverviewTool),
        nameof(ListBoardsTool),
        nameof(GetBoardSummaryTool),
        nameof(ListTasksTool),
        nameof(CountWorkspaceTasksTool),
        nameof(ListWorkspaceOverdueTasksTool),
        nameof(ListWorkspaceTasksDueSoonTool),
        nameof(GetMyPlanLimitsTool)
    ];

    [Theory]
    [MemberData(nameof(Tools))]
    public async Task Tool_refuses_before_reading_any_data(string tool)
    {
        var repository = new ThrowingRepository();

        await Assert.ThrowsAsync<ForbiddenException>(() => Invoke(tool, repository));

        Assert.False(repository.WasRead, $"{tool} read data before authorizing.");
    }

    private static Task Invoke(string tool, IAiDataRepository repository)
    {
        var workspace = new RefusingWorkspaceAccessService();
        var board = new RefusingBoardAccessService();
        var options = Options.Create(new AiToolOptions());
        var clock = new FixedCalendar();
        var id = Guid.NewGuid();

        return tool switch
        {
            nameof(ListMyWorkspacesTool) =>
                new ListMyWorkspacesToolHandler(repository, workspace)
                    .Handle(new ListMyWorkspacesTool(), default),
            nameof(GetWorkspaceOverviewTool) =>
                new GetWorkspaceOverviewToolHandler(repository, workspace)
                    .Handle(new GetWorkspaceOverviewTool(id), default),
            nameof(ListBoardsTool) =>
                new ListBoardsToolHandler(repository, workspace)
                    .Handle(new ListBoardsTool(id), default),
            nameof(GetBoardSummaryTool) =>
                new GetBoardSummaryToolHandler(repository, board, clock)
                    .Handle(new GetBoardSummaryTool(id), default),
            nameof(ListTasksTool) =>
                new ListTasksToolHandler(repository, board, options, clock)
                    .Handle(new ListTasksTool(id), default),
            nameof(CountWorkspaceTasksTool) =>
                new CountWorkspaceTasksToolHandler(repository, workspace, clock)
                    .Handle(new CountWorkspaceTasksTool(id), default),
            nameof(ListWorkspaceOverdueTasksTool) =>
                new ListWorkspaceOverdueTasksToolHandler(repository, workspace, options, clock)
                    .Handle(new ListWorkspaceOverdueTasksTool(id), default),
            nameof(ListWorkspaceTasksDueSoonTool) =>
                new ListWorkspaceTasksDueSoonToolHandler(repository, workspace, options, clock)
                    .Handle(new ListWorkspaceTasksDueSoonTool(id), default),
            nameof(GetMyPlanLimitsTool) =>
                new GetMyPlanLimitsToolHandler(repository, workspace, new UnusedPlanCatalog())
                    .Handle(new GetMyPlanLimitsTool(id), default),
            _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, "Unmapped tool.")
        };
    }

    private sealed class ThrowingRepository : IAiDataRepository
    {
        public bool WasRead { get; private set; }

        public Task<IReadOnlyList<AiWorkspaceSummary>> GetMyWorkspacesAsync(Guid c, CancellationToken ct = default) =>
            Fail<IReadOnlyList<AiWorkspaceSummary>>();

        public Task<AiWorkspaceUsage?> GetWorkspaceUsageAsync(Guid w, Guid c, CancellationToken ct = default) =>
            Fail<AiWorkspaceUsage?>();

        public Task<IReadOnlyList<AiBoardSummary>> GetBoardsAsync(
            Guid w, Guid c, bool a, CancellationToken ct = default) => Fail<IReadOnlyList<AiBoardSummary>>();

        public Task<AiBoardDetail?> GetBoardDetailAsync(
            Guid b, Guid c, DateTimeOffset asOf, CancellationToken ct = default) => Fail<AiBoardDetail?>();

        public Task<IReadOnlyList<AiTaskSummary>> GetBoardTasksAsync(
            Guid b, Guid c, AiTaskFilter f, CancellationToken ct = default) => Fail<IReadOnlyList<AiTaskSummary>>();

        public Task<IReadOnlyList<AiTaskSummary>> GetWorkspaceOverdueTasksAsync(
            Guid w, Guid c, DateTimeOffset asOf, int take, CancellationToken ct = default) =>
            Fail<IReadOnlyList<AiTaskSummary>>();

        public Task<IReadOnlyList<AiTaskSummary>> GetWorkspaceTasksDueSoonAsync(
            Guid w, Guid c, DateTimeOffset asOf, TimeSpan window, int take, CancellationToken ct = default) =>
            Fail<IReadOnlyList<AiTaskSummary>>();

        public Task<string?> GetWorkspacePlanIdAsync(Guid w, Guid c, CancellationToken ct = default) =>
            Fail<string?>();

        public Task<AiTaskCounts> CountWorkspaceTasksAsync(
            Guid w, Guid c, Guid? b, DateTimeOffset asOf, CancellationToken ct = default) => Fail<AiTaskCounts>();

        private Task<T> Fail<T>()
        {
            WasRead = true;
            throw new InvalidOperationException("Repository reached without authorization.");
        }
    }

    private sealed class RefusingWorkspaceAccessService : IWorkspaceAccessService
    {
        public Task<(Guid UserId, string Email)> GetCurrentUserInfoAsync(CancellationToken ct = default) => Refuse();

        public Task<(Guid UserId, string Email, WorkspaceRole Role)> EnsureIsMemberAsync(
            Guid workspaceId, CancellationToken ct = default) => throw Forbidden();

        public Task<(Guid UserId, string Email)> EnsureCanManageWorkspaceAsync(
            Guid workspaceId, CancellationToken ct = default) => Refuse();

        public Task EnsureCanManageMembersAsync(Guid workspaceId, CancellationToken ct = default) => throw Forbidden();

        public Task EnsureCanChangeMemberRoleAsync(Guid workspaceId, CancellationToken ct = default) =>
            throw Forbidden();

        public Task EnsureCanDeleteWorkspaceAsync(Guid workspaceId, CancellationToken ct = default) =>
            throw Forbidden();

        public Task EnsureCanManageInvitesAsync(Guid workspaceId, CancellationToken ct = default) => throw Forbidden();

        public Task EnsureCanManageBoardMembersAsync(Guid workspaceId, CancellationToken ct = default) =>
            throw Forbidden();

        public Task EnsureCanManageSubscriptionsAsync(Guid workspaceId, CancellationToken ct = default) =>
            throw Forbidden();

        public Task<(Guid UserId, string Email)> EnsureCanCurateTagsAsync(
            Guid workspaceId, CancellationToken ct = default) => Refuse();

        public Task<(Guid UserId, string Email)> EnsureCanViewStatsAsync(
            Guid workspaceId, CancellationToken ct = default) => Refuse();

        private static Task<(Guid, string)> Refuse() => throw Forbidden();
    }

    private sealed class RefusingBoardAccessService : IBoardAccessService
    {
        public Task<(Guid UserId, string Email)> GetCurrentUserAsync(CancellationToken ct = default) =>
            throw Forbidden();

        public Task<BoardAccessContext> EnsureCanEditBoardAsync(Guid b, CancellationToken ct = default) =>
            throw Forbidden();

        public Task<BoardAccessContext> EnsureCanDeleteBoardAsync(Guid b, CancellationToken ct = default) =>
            throw Forbidden();

        public Task<BoardAccessContext> EnsureCanManageColumnsAsync(Guid b, CancellationToken ct = default) =>
            throw Forbidden();

        public Task<BoardAccessContext> EnsureCanManageTasksAsync(Guid b, CancellationToken ct = default) =>
            throw Forbidden();

        public Task<BoardAccessContext> EnsureCanCompleteTasksAsync(Guid b, CancellationToken ct = default) =>
            throw Forbidden();

        public Task<BoardAccessContext> EnsureCanTagTasksAsync(Guid b, CancellationToken ct = default) =>
            throw Forbidden();

        public Task<BoardAccessContext> EnsureCanManageCommentsAsync(Guid b, CancellationToken ct = default) =>
            throw Forbidden();

        public Task<BoardAccessContext> EnsureCanManageAttachmentsAsync(Guid b, CancellationToken ct = default) =>
            throw Forbidden();

        public Task<BoardAccessContext> EnsureCanViewBoardAsync(Guid b, CancellationToken ct = default) =>
            throw Forbidden();

        public Task<BoardAccessContext> EnsureCanExportBoardAsync(Guid b, CancellationToken ct = default) =>
            throw Forbidden();

        public Task<BoardAccessContext> EnsureCanStartCallAsync(Guid b, CancellationToken ct = default) =>
            throw Forbidden();

        public Task<BoardAccessContext> EnsureCanEndCallAsync(Guid b, CancellationToken ct = default) =>
            throw Forbidden();

        public Task<Contracts.Enums.BoardRoleDto?> GetEffectiveBoardRoleAsync(
            Guid b, CancellationToken ct = default) => throw Forbidden();
    }

    private sealed class UnusedPlanCatalog : IPlanCatalog
    {
        public string DefaultPlanId => throw new InvalidOperationException("Plan catalog reached.");

        public PlanDto GetPlan(string planId) => throw new InvalidOperationException("Plan catalog reached.");

        public IReadOnlyList<PlanDto> GetAllPlans() => throw new InvalidOperationException("Plan catalog reached.");

        public string? TryGetPriceId(string planId) => throw new InvalidOperationException("Plan catalog reached.");

        public string GetPriceId(string planId) => throw new InvalidOperationException("Plan catalog reached.");

        public WorkspaceLimits GetLimits(string planId) =>
            throw new InvalidOperationException("Plan catalog reached.");
    }

    // The tools take a calendar now, not a clock: overdue is a day in the configured zone, not an instant.
    private sealed class FixedCalendar : IBusinessCalendar
    {
        public string TimeZoneId => "UTC";

        public DateOnly Today => new(2026, 06, 15);

        public DateTimeOffset StartOfDayUtc(DateOnly date) => new(date, TimeOnly.MinValue, TimeSpan.Zero);

        public DateTimeOffset StartOfTodayUtc() => StartOfDayUtc(Today);

        public DateTimeOffset StartOfDayLocal(DateOnly date) => StartOfDayUtc(date);

        public DateOnly ToLocalDate(DateTimeOffset instant) => DateOnly.FromDateTime(instant.UtcDateTime);

        public int DaysOverdue(DateTimeOffset dueDate) => Today.DayNumber - ToLocalDate(dueDate).DayNumber;
    }

    private static ForbiddenException Forbidden() => new("Not a member.");
}
