using ArchitectureTests.Ai;
using Domain.Authorization;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ArchitectureTests.Tasks;

public class TaskCompletionSchemaTests(AiSentinelFixture fixture) : IClassFixture<AiSentinelFixture>
{
    [Fact]
    public async Task Every_pre_existing_task_is_uncompleted()
    {
        var tasks = await fixture.Context.Tasks.AsNoTracking().ToListAsync();

        Assert.NotEmpty(tasks);
        Assert.All(tasks, task => Assert.False(task.IsCompleted));
        Assert.All(tasks, task => Assert.Null(task.CompletedAt));
        Assert.All(tasks, task => Assert.Null(task.CompletedById));
    }

    [Fact]
    public async Task Completing_a_task_stamps_when_and_by_whom()
    {
        var task = await AddTaskAsync();

        task.IsCompleted = true;
        task.CompletedAt = fixture.AsOf;
        task.CompletedById = fixture.CallerId;
        await fixture.Context.SaveChangesAsync();

        var stored = await Reload(task.Id);

        Assert.True(stored.IsCompleted);
        Assert.Equal(fixture.AsOf, stored.CompletedAt);
        Assert.Equal(fixture.CallerId, stored.CompletedById);
    }

    [Fact]
    public async Task Reopening_clears_both_stamps()
    {
        var task = await AddTaskAsync(completed: true);

        task.IsCompleted = false;
        task.CompletedAt = null;
        task.CompletedById = null;
        await fixture.Context.SaveChangesAsync();

        var stored = await Reload(task.Id);

        Assert.False(stored.IsCompleted);
        Assert.Null(stored.CompletedAt);
        Assert.Null(stored.CompletedById);
    }

    [Fact]
    public async Task A_completed_task_without_a_timestamp_is_rejected_by_the_database()
    {
        var task = await AddTaskAsync();

        task.IsCompleted = true;

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Context.SaveChangesAsync());

        Assert.Contains("CK_Tasks_Completion_Consistent", error.InnerException?.Message ?? "", StringComparison.Ordinal);
        fixture.Context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task An_uncompleted_task_carrying_a_timestamp_is_rejected_by_the_database()
    {
        var task = await AddTaskAsync();

        task.CompletedAt = fixture.AsOf;

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Context.SaveChangesAsync());

        Assert.Contains("CK_Tasks_Completion_Consistent", error.InnerException?.Message ?? "", StringComparison.Ordinal);
        fixture.Context.ChangeTracker.Clear();
    }

    [Theory]
    [InlineData(BoardRole.Admin, true)]
    [InlineData(BoardRole.ScrumMaster, true)]
    [InlineData(BoardRole.User, true)]
    public void Completing_is_permitted_wherever_moving_is(BoardRole role, bool expected)
    {
        Assert.Equal(expected, BoardRolePermissions.CanCompleteTasks(role));
        Assert.Equal(BoardRolePermissions.CanMoveTasks(role), BoardRolePermissions.CanCompleteTasks(role));
    }

    [Fact]
    public void Completing_is_deliberately_looser_than_editing()
    {
        Assert.True(BoardRolePermissions.CanCompleteTasks(BoardRole.User));
        Assert.False(BoardRolePermissions.CanManageTasks(BoardRole.User));
    }

    private async Task<TaskItem> AddTaskAsync(bool completed = false)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            ColumnId = fixture.ColumnId,
            Title = "completion probe",
            ReporterId = fixture.CallerId,
            Position = 99,
            IsCompleted = completed,
            CompletedAt = completed ? fixture.AsOf : null,
            CompletedById = completed ? fixture.CallerId : null
        };

        fixture.Context.Tasks.Add(task);
        await fixture.Context.SaveChangesAsync();

        return task;
    }

    private async Task<TaskItem> Reload(Guid id) =>
        await fixture.Context.Tasks.AsNoTracking().SingleAsync(task => task.Id == id);
}
