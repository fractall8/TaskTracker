using System.Text.Json;
using Application.Interfaces.Repositories;
using Persistence.Repositories;

namespace ArchitectureTests.Ai;

// The behavioural backstop to the approved manifest: the manifest cannot tell that a field named
// CreatorRef holds a user id, so every repository result is serialized and searched for seeded identity.
public class AiDataRepositoryLeakTests(AiSentinelFixture fixture) : IClassFixture<AiSentinelFixture>
{
    private IAiDataRepository Repository => new AiDataRepository(fixture.Context);

    public static TheoryData<string> AllReads =>
    [
        nameof(IAiDataRepository.GetMyWorkspacesAsync),
        nameof(IAiDataRepository.GetWorkspaceUsageAsync),
        nameof(IAiDataRepository.GetBoardsAsync),
        nameof(IAiDataRepository.GetBoardDetailAsync),
        nameof(IAiDataRepository.GetBoardTasksAsync),
        nameof(IAiDataRepository.GetWorkspaceOverdueTasksAsync),
        nameof(IAiDataRepository.CountWorkspaceTasksAsync)
    ];

    [Theory]
    [MemberData(nameof(AllReads))]
    public async Task Read_leaks_no_seeded_identity(string read)
    {
        var payload = JsonSerializer.Serialize(await Invoke(read));

        foreach (var sentinel in fixture.Sentinels)
        {
            Assert.DoesNotContain(sentinel, payload, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Tasks_are_summarised_without_description_or_assignee()
    {
        var tasks = await Repository.GetBoardTasksAsync(fixture.BoardId, fixture.CallerId, new AiTaskFilter());

        Assert.Equal(3, tasks.Count);
        Assert.Contains(tasks, task => task is { IsAssignedToMe: true, IsAssigned: true });
        Assert.Contains(tasks, task => task is { IsAssignedToMe: false, IsAssigned: true });
        Assert.Contains(tasks, task => !task.IsAssigned);
        Assert.All(tasks, task => Assert.Equal("In Progress", task.ColumnName));
    }

    [Fact]
    public async Task Board_the_caller_is_not_a_member_of_is_invisible()
    {
        var boards = await Repository.GetBoardsAsync(fixture.WorkspaceId, fixture.CallerId, includeArchived: true);

        Assert.Single(boards);
        Assert.Equal(fixture.BoardId, boards[0].Id);
        Assert.Null(await Repository.GetBoardDetailAsync(
            fixture.InvisibleBoardId, fixture.CallerId, fixture.AsOf));
    }

    // MyBoardCount counts only boards the caller can see; AiWorkspaceUsage counts workspace totals for
    // plan limits. The difference is deliberate — asserted so neither is "fixed" to match the other.
    [Fact]
    public async Task Workspace_counts_distinguish_visible_from_total()
    {
        var mine = await Repository.GetMyWorkspacesAsync(fixture.CallerId);
        var usage = await Repository.GetWorkspaceUsageAsync(fixture.WorkspaceId, fixture.CallerId);

        Assert.Equal(1, Assert.Single(mine).MyBoardCount);
        Assert.NotNull(usage);
        Assert.Equal(2, usage.BoardCount);
        Assert.Equal(0, usage.ArchivedBoardCount);
        Assert.Equal(2, usage.MemberCount);
    }

    [Fact]
    public async Task Overdue_read_returns_only_overdue_tasks()
    {
        var overdue = await Repository.GetWorkspaceOverdueTasksAsync(
            fixture.WorkspaceId, fixture.CallerId, fixture.AsOf, take: 25);

        Assert.Equal("Overdue and mine", Assert.Single(overdue).Title);
    }

    [Fact]
    public async Task Non_member_sees_nothing()
    {
        Assert.Empty(await Repository.GetMyWorkspacesAsync(fixture.OutsiderId));
        Assert.Null(await Repository.GetWorkspaceUsageAsync(fixture.WorkspaceId, fixture.OutsiderId));
        Assert.Empty(await Repository.GetBoardsAsync(fixture.WorkspaceId, fixture.OutsiderId, true));
        Assert.Empty(await Repository.GetBoardTasksAsync(fixture.BoardId, fixture.OutsiderId, new AiTaskFilter()));
    }

    [Fact]
    public async Task Counts_are_computed_against_the_supplied_clock()
    {
        var counts = await Repository.CountWorkspaceTasksAsync(
            fixture.WorkspaceId, fixture.CallerId, boardId: null, fixture.AsOf);

        Assert.Equal(3, counts.Total);
        Assert.Equal(1, counts.Overdue);
        Assert.Equal(1, counts.DueThisWeek);
        Assert.Equal(1, counts.AssignedToMe);
    }

    [Fact]
    public async Task Board_detail_reports_role_and_per_column_counts()
    {
        var detail = await Repository.GetBoardDetailAsync(fixture.BoardId, fixture.CallerId, fixture.AsOf);

        Assert.NotNull(detail);
        Assert.Equal(Contracts.Enums.BoardRoleDto.Admin, detail.MyBoardRole);
        Assert.Equal(3, detail.TaskCount);
        Assert.Equal(1, detail.OverdueTaskCount);
        Assert.Equal(1, detail.UnassignedTaskCount);
        Assert.Single(detail.Columns);
        Assert.Equal(3, detail.Columns[0].TaskCount);
    }

    [Fact]
    public async Task Take_is_clamped()
    {
        var tasks = await Repository.GetBoardTasksAsync(
            fixture.BoardId, fixture.CallerId, new AiTaskFilter(Take: 1));

        Assert.Single(tasks);
    }

    private async Task<object?> Invoke(string read) => read switch
    {
        nameof(IAiDataRepository.GetMyWorkspacesAsync) =>
            await Repository.GetMyWorkspacesAsync(fixture.CallerId),
        nameof(IAiDataRepository.GetWorkspaceUsageAsync) =>
            await Repository.GetWorkspaceUsageAsync(fixture.WorkspaceId, fixture.CallerId),
        nameof(IAiDataRepository.GetBoardsAsync) =>
            await Repository.GetBoardsAsync(fixture.WorkspaceId, fixture.CallerId, includeArchived: true),
        nameof(IAiDataRepository.GetBoardDetailAsync) =>
            await Repository.GetBoardDetailAsync(fixture.BoardId, fixture.CallerId, fixture.AsOf),
        nameof(IAiDataRepository.GetBoardTasksAsync) =>
            await Repository.GetBoardTasksAsync(fixture.BoardId, fixture.CallerId, new AiTaskFilter()),
        nameof(IAiDataRepository.GetWorkspaceOverdueTasksAsync) =>
            await Repository.GetWorkspaceOverdueTasksAsync(fixture.WorkspaceId, fixture.CallerId, fixture.AsOf, 25),
        nameof(IAiDataRepository.CountWorkspaceTasksAsync) =>
            await Repository.CountWorkspaceTasksAsync(fixture.WorkspaceId, fixture.CallerId, null, fixture.AsOf),
        _ => throw new ArgumentOutOfRangeException(nameof(read), read, "Unmapped repository read.")
    };
}
