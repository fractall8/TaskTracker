using Application.Features.Tasks.Commands;
using ArchitectureTests.Ai;
using Contracts.Notifications.BoardActions;
using Contracts.Notifications.BoardActions.Payloads;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Persistence.Repositories;

namespace ArchitectureTests.Tasks;

public class TaskCompletionCommandTests(AiSentinelFixture fixture) : IClassFixture<AiSentinelFixture>
{
    [Fact]
    public async Task Completing_stamps_when_and_by_whom_and_persists()
    {
        var task = await AddTaskAsync();
        var notifier = new RecordingNotifier();

        var dto = await Handler(notifier).Handle(new CompleteTaskCommand(fixture.BoardId, task.Id), default);

        var stored = await Reload(task.Id);

        Assert.True(stored.IsCompleted);
        Assert.Equal(fixture.AsOf, stored.CompletedAt);
        Assert.Equal(fixture.CallerId, stored.CompletedById);
        Assert.True(dto.IsCompleted);
        Assert.Equal(fixture.AsOf, dto.CompletedAt);
    }

    [Fact]
    public async Task Completing_announces_the_change_to_the_board()
    {
        var task = await AddTaskAsync();
        var notifier = new RecordingNotifier();

        await Handler(notifier).Handle(new CompleteTaskCommand(fixture.BoardId, task.Id), default);

        var notification = Assert.Single(notifier.Sent);
        Assert.Equal(BoardActionNotificationType.TaskCompletionChanged, notification.Type);

        var payload = Assert.IsType<TaskCompletionChangedPayload>(notification.Payload);
        Assert.Equal(task.Id, payload.TaskId);
        Assert.True(payload.IsCompleted);
    }

    [Fact]
    public async Task Completing_twice_keeps_the_original_timestamp_and_stays_quiet()
    {
        var task = await AddTaskAsync(completed: true, completedAt: fixture.AsOf.AddDays(-5));
        var notifier = new RecordingNotifier();

        await Handler(notifier).Handle(new CompleteTaskCommand(fixture.BoardId, task.Id), default);

        var stored = await Reload(task.Id);

        Assert.Equal(fixture.AsOf.AddDays(-5), stored.CompletedAt);
        Assert.Empty(notifier.Sent);
    }

    [Fact]
    public async Task Reopening_clears_every_completion_stamp()
    {
        var task = await AddTaskAsync(completed: true);
        var notifier = new RecordingNotifier();

        var dto = await ReopenHandler(notifier).Handle(new ReopenTaskCommand(fixture.BoardId, task.Id), default);

        var stored = await Reload(task.Id);

        Assert.False(stored.IsCompleted);
        Assert.Null(stored.CompletedAt);
        Assert.Null(stored.CompletedById);
        Assert.False(dto.IsCompleted);
        Assert.Null(dto.CompletedAt);
    }

    [Fact]
    public async Task Reopening_an_open_task_stays_quiet()
    {
        var task = await AddTaskAsync();
        var notifier = new RecordingNotifier();

        await ReopenHandler(notifier).Handle(new ReopenTaskCommand(fixture.BoardId, task.Id), default);

        Assert.Empty(notifier.Sent);
    }

    [Fact]
    public async Task A_task_on_another_board_cannot_be_completed_through_a_board_the_caller_does_hold()
    {
        var foreign = await AddTaskAsync(columnId: fixture.ForeignColumnId);
        var notifier = new RecordingNotifier();

        // Authorization passes for fixture.BoardId; only the ownership check stands between the
        // caller and a task in a tenant they are not a member of.
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Handler(notifier).Handle(new CompleteTaskCommand(fixture.BoardId, foreign.Id), default));

        var stored = await Reload(foreign.Id);

        Assert.False(stored.IsCompleted);
        Assert.Empty(notifier.Sent);
    }

    [Fact]
    public async Task A_task_on_another_board_cannot_be_reopened_either()
    {
        var foreign = await AddTaskAsync(columnId: fixture.ForeignColumnId, completed: true);
        var notifier = new RecordingNotifier();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            ReopenHandler(notifier).Handle(new ReopenTaskCommand(fixture.BoardId, foreign.Id), default));

        Assert.True((await Reload(foreign.Id)).IsCompleted);
        Assert.Empty(notifier.Sent);
    }

    [Fact]
    public async Task An_unknown_task_is_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Handler(new RecordingNotifier())
                .Handle(new CompleteTaskCommand(fixture.BoardId, fixture.FabricatedId), default));
    }

    [Fact]
    public async Task Authorization_runs_before_the_task_is_ever_read()
    {
        var task = await AddTaskAsync();
        var notifier = new RecordingNotifier();

        var handler = new CompleteTaskCommandHandler(
            new DenyingAccessService(),
            new TaskRepository(fixture.Context),
            notifier,
            new FixedClock(fixture.AsOf),
            new ContextUnitOfWork(fixture.Context));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new CompleteTaskCommand(fixture.BoardId, task.Id), default));

        Assert.False((await Reload(task.Id)).IsCompleted);
        Assert.Empty(notifier.Sent);
    }

    private CompleteTaskCommandHandler Handler(RecordingNotifier notifier) =>
        new(
            new GrantingAccessService(fixture.CallerId),
            new TaskRepository(fixture.Context),
            notifier,
            new FixedClock(fixture.AsOf),
            new ContextUnitOfWork(fixture.Context));

    private ReopenTaskCommandHandler ReopenHandler(RecordingNotifier notifier) =>
        new(
            new GrantingAccessService(fixture.CallerId),
            new TaskRepository(fixture.Context),
            notifier,
            new FixedClock(fixture.AsOf),
            new ContextUnitOfWork(fixture.Context));

    private async Task<TaskItem> AddTaskAsync(
        Guid? columnId = null,
        bool completed = false,
        DateTimeOffset? completedAt = null)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            ColumnId = columnId ?? fixture.ColumnId,
            Title = "completion command probe",
            ReporterId = fixture.CallerId,
            Position = 500,
            IsCompleted = completed,
            CompletedAt = completed ? completedAt ?? fixture.AsOf : null,
            CompletedById = completed ? fixture.CallerId : null
        };

        fixture.Context.Tasks.Add(task);
        await fixture.Context.SaveChangesAsync();
        fixture.Context.ChangeTracker.Clear();

        return task;
    }

    private async Task<TaskItem> Reload(Guid id) =>
        await fixture.Context.Tasks.AsNoTracking().SingleAsync(task => task.Id == id);
}
