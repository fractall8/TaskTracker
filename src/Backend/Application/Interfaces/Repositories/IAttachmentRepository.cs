using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IAttachmentRepository : IRepository<Attachment, Guid>
{
    Task<List<string>> GetUrlsByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default);

    Task<List<string>> GetUrlsByBoardIdAsync(Guid boardId, CancellationToken ct = default);

    Task<List<string>> GetUrlsByColumnIdAsync(Guid columnId, CancellationToken ct = default);

    Task<List<string>> GetUrlsByTaskIdAsync(Guid taskId, CancellationToken ct = default);

    Task<List<Attachment>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default);
}
