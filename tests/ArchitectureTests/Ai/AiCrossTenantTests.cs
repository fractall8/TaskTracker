using Application.Interfaces.Repositories;
using Persistence.Repositories;

namespace ArchitectureTests.Ai;

public class AiCrossTenantTests(AiSentinelFixture fixture) : IClassFixture<AiSentinelFixture>
{
    private IAiDataRepository Repository => new AiDataRepository(fixture.Context);

    public static TheoryData<string> ForeignIdKinds => ["foreign-tenant", "fabricated"];

    private Guid WorkspaceIdFor(string kind) =>
        kind == "foreign-tenant" ? fixture.ForeignWorkspaceId : fixture.FabricatedId;

    private Guid BoardIdFor(string kind) =>
        kind == "foreign-tenant" ? fixture.ForeignBoardId : fixture.FabricatedId;

    [Theory]
    [MemberData(nameof(ForeignIdKinds))]
    public async Task Workspace_reads_reject_an_id_outside_the_callers_tenancy(string kind)
    {
        var workspaceId = WorkspaceIdFor(kind);

        Assert.Null(await Repository.GetWorkspaceUsageAsync(workspaceId, fixture.CallerId));
        Assert.Null(await Repository.GetWorkspacePlanIdAsync(workspaceId, fixture.CallerId));
        Assert.Empty(await Repository.GetBoardsAsync(workspaceId, fixture.CallerId, includeArchived: true));
        Assert.Empty(await Repository.GetWorkspaceOverdueTasksAsync(
            workspaceId, fixture.CallerId, fixture.AsOf, take: 25));
        Assert.Empty(await Repository.GetWorkspaceTasksDueSoonAsync(
            workspaceId, fixture.CallerId, fixture.AsOf, TimeSpan.FromDays(7), take: 25));

        var counts = await Repository.CountWorkspaceTasksAsync(
            workspaceId, fixture.CallerId, boardId: null, fixture.AsOf);

        Assert.Equal(0, counts.Total);
    }

    [Theory]
    [MemberData(nameof(ForeignIdKinds))]
    public async Task Board_reads_reject_an_id_outside_the_callers_tenancy(string kind)
    {
        var boardId = BoardIdFor(kind);

        Assert.Null(await Repository.GetBoardDetailAsync(boardId, fixture.CallerId, fixture.AsOf));
        Assert.Empty(await Repository.GetBoardTasksAsync(boardId, fixture.CallerId, new AiTaskFilter()));
    }

    [Fact]
    public async Task The_foreign_tenant_is_real_so_the_rejection_is_not_a_missing_row()
    {
        // Without this the tests above would pass against an empty database and prove nothing.
        var theirs = await Repository.GetBoardTasksAsync(
            fixture.ForeignBoardId, fixture.OutsiderId, new AiTaskFilter());

        Assert.Equal(AiSentinelFixture.ForeignTaskTitle, Assert.Single(theirs).Title);
        Assert.NotNull(await Repository.GetWorkspaceUsageAsync(fixture.ForeignWorkspaceId, fixture.OutsiderId));
    }

    [Fact]
    public async Task Counting_a_foreign_board_inside_my_own_workspace_yields_nothing()
    {
        var counts = await Repository.CountWorkspaceTasksAsync(
            fixture.WorkspaceId, fixture.CallerId, fixture.ForeignBoardId, fixture.AsOf);

        Assert.Equal(0, counts.Total);
    }

    [Fact]
    public async Task An_injection_payload_in_a_task_title_is_returned_as_inert_data()
    {
        var tasks = await Repository.GetBoardTasksAsync(
            fixture.BoardId, fixture.CallerId, new AiTaskFilter(Take: 25));

        // The title comes back verbatim — sanitising it would hide the attack rather than contain it. What
        // must hold is that reading it changed nothing: the caller still sees only their own board.
        Assert.Contains(tasks, task => task.Title == AiSentinelFixture.InjectionTitle);
        Assert.All(tasks, task => Assert.Equal("In Progress", task.ColumnName));
        Assert.DoesNotContain(tasks, task => task.Title == AiSentinelFixture.ForeignTaskTitle);
    }

    [Fact]
    public async Task A_foreign_id_embedded_in_a_task_title_is_still_rejected_when_used()
    {
        var tasks = await Repository.GetBoardTasksAsync(
            fixture.BoardId, fixture.CallerId, new AiTaskFilter(Take: 25));

        var planted = Assert.Single(
            tasks.Where(task => task.Title.Contains(fixture.ForeignBoardId.ToString(), StringComparison.Ordinal)));

        Assert.Contains("URGENT", planted.Title, StringComparison.Ordinal);

        // Even if the model lifted that id straight out of the title, authorization is what stops it.
        Assert.Null(await Repository.GetBoardDetailAsync(
            fixture.ForeignBoardId, fixture.CallerId, fixture.AsOf));
    }
}
