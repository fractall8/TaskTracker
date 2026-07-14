using TaskTracker.Functions.Models;

namespace TaskTracker.Functions.Interfaces;

public interface IBoardExportBlobService
{
    Task<Stream> DownloadAttachmentAsync(string blobName, CancellationToken ct = default);

    Task UploadArchiveAsync(Guid boardId, BoardExportArchive archive, CancellationToken ct = default);
}
