using Application.Features.Tasks.Commands;
using ArchitectureTests.Ai;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Persistence.Repositories;

namespace ArchitectureTests.Tasks;

public class UpdateTaskDueDateCommandTests(AiSentinelFixture fixture) : IClassFixture<AiSentinelFixture>
{
    [Fact]
    public async Task A_due_date_on_the_callers_own_board_is_updated()
    {
        var task = await AddTaskAsync();
        var notifier = new RecordingNotifier();
        var newDueDate = fixture.AsOf.AddDays(7);

        var dto = await Handler(notifier).Handle(
            new UpdateTaskDueDateCommand(fixture.BoardId, task.Id, newDueDate), default);

        Assert.Equal(newDueDate, (await Reload(task.Id)).DueDate);
        Assert.Equal(newDueDate, dto.DueDate);
    }

    [Fact]
    public async Task A_task_in_a_foreign_tenant_cannot_have_its_due_date_changed()
    {
        var foreign = await AddTaskAsync(columnId: fixture.ForeignColumnId, dueDate: fixture.AsOf);
        var notifier = new RecordingNotifier();

        // Authorization passes for fixture.BoardId; only the ownership check stands between the
        // caller and a task in a tenant they are not a member of.
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Handler(notifier).Handle(
                new UpdateTaskDueDateCommand(fixture.BoardId, foreign.Id, fixture.AsOf.AddYears(1)), default));

        Assert.Equal(fixture.AsOf, (await Reload(foreign.Id)).DueDate);
        Assert.Empty(notifier.Sent);
    }

    [Fact]
    public async Task The_returned_task_carries_the_people_on_it()
    {
        var task = await AddTaskAsync(assigneeId: fixture.CallerId);

        var dto = await Handler(new RecordingNotifier()).Handle(
            new UpdateTaskDueDateCommand(fixture.BoardId, task.Id, fixture.AsOf), default);

        Assert.Equal(AiSentinelFixture.CallerDisplayName, dto.AssigneeName);
        Assert.Equal(AiSentinelFixture.CallerDisplayName, dto.ReporterName);
    }

    private UpdateTaskDueDateCommandHandler Handler(RecordingNotifier notifier) =>
        new(
            new GrantingAccessService(fixture.CallerId),
            new TaskRepository(fixture.Context),
            notifier,
            new FixedClock(fixture.AsOf),
            new ContextUnitOfWork(fixture.Context));

    private async Task<TaskItem> AddTaskAsync(
        Guid? columnId = null,
        Guid? assigneeId = null,
        DateTimeOffset? dueDate = null)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            ColumnId = columnId ?? fixture.ColumnId,
            Title = "due date probe",
            ReporterId = fixture.CallerId,
            AssigneeId = assigneeId,
            DueDate = dueDate,
            Position = 600
        };

        fixture.Context.Tasks.Add(task);
        await fixture.Context.SaveChangesAsync();
        fixture.Context.ChangeTracker.Clear();

        return task;
    }

    private async Task<TaskItem> Reload(Guid id) =>
        await fixture.Context.Tasks.AsNoTracking().SingleAsync(task => task.Id == id);
}
