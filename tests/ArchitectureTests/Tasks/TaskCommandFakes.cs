using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Interfaces.Notifiers;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using Contracts.Enums;
using Contracts.Notifications.BoardActions;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ArchitectureTests.Tasks;

internal sealed class RecordingNotifier : IBoardActionNotifier
{
    public List<BoardActionNotification> Sent { get; } = [];

    public Task NotifyAsync(BoardActionNotification notification, CancellationToken ct)
    {
        Sent.Add(notification);
        return Task.CompletedTask;
    }
}

internal sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; } = now;
}

internal sealed class ContextUnitOfWork(DbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default) => action(cancellationToken);

    public Task AcquireDistributedLockAsync(string lockKey, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

// Every member throws so a handler reaching for an unexpected permission fails loudly
// instead of silently sailing past authorization.
internal class StubAccessService : IBoardAccessService
{
    public virtual Task<BoardAccessContext> EnsureCanCompleteTasksAsync(Guid boardId, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public virtual Task<BoardAccessContext> EnsureCanManageTasksAsync(Guid boardId, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<(Guid UserId, string Email)> GetCurrentUserAsync(CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<BoardAccessContext> EnsureCanEditBoardAsync(Guid boardId, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<BoardAccessContext> EnsureCanDeleteBoardAsync(Guid boardId, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<BoardAccessContext> EnsureCanManageColumnsAsync(Guid boardId, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<BoardAccessContext> EnsureCanManageCommentsAsync(Guid boardId, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<BoardAccessContext> EnsureCanManageAttachmentsAsync(Guid boardId, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<BoardAccessContext> EnsureCanViewBoardAsync(Guid boardId, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<BoardAccessContext> EnsureCanExportBoardAsync(Guid boardId, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<BoardAccessContext> EnsureCanStartCallAsync(Guid boardId, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<BoardAccessContext> EnsureCanEndCallAsync(Guid boardId, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<BoardRoleDto?> GetEffectiveBoardRoleAsync(Guid boardId, CancellationToken ct = default) =>
        throw new NotSupportedException();
}

internal sealed class GrantingAccessService(Guid userId, BoardRole role = BoardRole.Admin) : StubAccessService
{
    public override Task<BoardAccessContext> EnsureCanCompleteTasksAsync(Guid boardId, CancellationToken ct = default) =>
        Task.FromResult(new BoardAccessContext(userId, role));

    public override Task<BoardAccessContext> EnsureCanManageTasksAsync(Guid boardId, CancellationToken ct = default) =>
        Task.FromResult(new BoardAccessContext(userId, role));
}

internal sealed class DenyingAccessService : StubAccessService
{
    public override Task<BoardAccessContext> EnsureCanCompleteTasksAsync(Guid boardId, CancellationToken ct = default) =>
        throw new ForbiddenException("Denied.");

    public override Task<BoardAccessContext> EnsureCanManageTasksAsync(Guid boardId, CancellationToken ct = default) =>
        throw new ForbiddenException("Denied.");
}
