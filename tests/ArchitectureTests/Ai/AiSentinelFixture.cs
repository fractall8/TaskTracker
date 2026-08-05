using Database;
using DbUp;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;
using Serilog.Core;
using Testcontainers.PostgreSql;

namespace ArchitectureTests.Ai;

public sealed class AiSentinelFixture : IAsyncLifetime
{
    public const string OtherUserDisplayName = "SENTINEL-OTHER-DISPLAYNAME";
    public const string OtherUserEmail = "sentinel-other@leak.invalid";
    public const string CallerDisplayName = "SENTINEL-CALLER-DISPLAYNAME";
    public const string CallerEmail = "sentinel-caller@leak.invalid";
    public const string OutsiderBoardName = "OUTSIDER-BOARD";
    public const string SecretDescription = "SENTINEL-TASK-DESCRIPTION";

    // Must match docker-compose.yml — testing on a different major version defeats the point.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    private TaskTrackerDbContext? _context;

    public TaskTrackerDbContext Context =>
        _context ?? throw new InvalidOperationException("Fixture not initialised.");

    public DateTimeOffset AsOf { get; } = new(2026, 06, 15, 12, 0, 0, TimeSpan.Zero);

    public Guid CallerId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Guid OtherUserId { get; } = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public Guid OutsiderId { get; } = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public Guid WorkspaceId { get; } = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    public Guid BoardId { get; } = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

    public Guid InvisibleBoardId { get; } = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    public Guid ColumnId { get; } = Guid.Parse("cccccccc-0000-0000-0000-000000000001");

    // If any of these appears in a projection's output, user identity escaped.
    public IReadOnlyList<string> Sentinels =>
    [
        OtherUserDisplayName, OtherUserEmail, CallerDisplayName, CallerEmail,
        SecretDescription, OutsiderBoardName,
        CallerId.ToString(), OtherUserId.ToString(), OutsiderId.ToString()
    ];

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();

        var upgrade = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(SerilogUpgradeLog).Assembly)
            .LogTo(new SerilogUpgradeLog(Logger.None))
            .Build()
            .PerformUpgrade();

        if (!upgrade.Successful)
        {
            throw new InvalidOperationException($"DbUp migration failed: {upgrade.Error}");
        }

        _context = new TaskTrackerDbContext(
            new DbContextOptionsBuilder<TaskTrackerDbContext>().UseNpgsql(connectionString).Options);

        await SeedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_context is not null)
        {
            await _context.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    private async Task SeedAsync()
    {
        Context.Users.AddRange(
            new User
            {
                Id = CallerId, AzureAdObjectId = CallerId, Email = CallerEmail, DisplayName = CallerDisplayName
            },
            new User
            {
                Id = OtherUserId, AzureAdObjectId = OtherUserId, Email = OtherUserEmail,
                DisplayName = OtherUserDisplayName
            },
            new User
            {
                Id = OutsiderId, AzureAdObjectId = OutsiderId, Email = "outsider@leak.invalid",
                DisplayName = "OUTSIDER"
            });

        Context.Workspaces.Add(new Workspace { Id = WorkspaceId, Name = "Sentinel Workspace" });

        var callerMembership = new WorkspaceMember
        {
            Id = Guid.NewGuid(), WorkspaceId = WorkspaceId, UserId = CallerId, Role = WorkspaceRole.Member, JoinedAt = AsOf
        };

        var otherMembership = new WorkspaceMember
        {
            Id = Guid.NewGuid(), WorkspaceId = WorkspaceId, UserId = OtherUserId, Role = WorkspaceRole.Owner, JoinedAt = AsOf
        };

        Context.WorkspaceMembers.AddRange(callerMembership, otherMembership);

        Context.Boards.AddRange(
            new Board { Id = BoardId, WorkspaceId = WorkspaceId, Name = "Sentinel Board" },
            new Board { Id = InvisibleBoardId, WorkspaceId = WorkspaceId, Name = OutsiderBoardName });

        // The caller is a member of one board only; the second exists to prove it stays invisible.
        Context.BoardMembers.AddRange(
            new BoardMember
            {
                Id = Guid.NewGuid(), BoardId = BoardId, WorkspaceMemberId = callerMembership.Id,
                Role = BoardRole.Admin, JoinedAt = AsOf
            },
            new BoardMember
            {
                Id = Guid.NewGuid(), BoardId = BoardId, WorkspaceMemberId = otherMembership.Id,
                Role = BoardRole.User, JoinedAt = AsOf
            },
            new BoardMember
            {
                Id = Guid.NewGuid(), BoardId = InvisibleBoardId, WorkspaceMemberId = otherMembership.Id,
                Role = BoardRole.Admin, JoinedAt = AsOf
            });

        Context.Columns.Add(new Column { Id = ColumnId, BoardId = BoardId, Name = "In Progress", Position = 0 });

        Context.Tasks.AddRange(
            new TaskItem
            {
                Id = Guid.NewGuid(), ColumnId = ColumnId, Title = "Overdue and mine",
                Description = SecretDescription, AssigneeId = CallerId, ReporterId = OtherUserId,
                DueDate = AsOf.AddDays(-3), Position = 0
            },
            new TaskItem
            {
                Id = Guid.NewGuid(), ColumnId = ColumnId, Title = "Due this week, someone else's",
                Description = SecretDescription, AssigneeId = OtherUserId, ReporterId = OtherUserId,
                DueDate = AsOf.AddDays(2), Position = 1
            },
            new TaskItem
            {
                Id = Guid.NewGuid(), ColumnId = ColumnId, Title = "Unassigned, no due date",
                Description = SecretDescription, ReporterId = CallerId, Position = 2
            });

        await Context.SaveChangesAsync();
    }
}
