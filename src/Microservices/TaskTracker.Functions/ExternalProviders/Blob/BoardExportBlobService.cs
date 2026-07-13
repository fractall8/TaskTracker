using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskTracker.Functions.Interfaces;
using TaskTracker.Functions.Models;

namespace TaskTracker.Functions.ExternalProviders.Blob;

public sealed class BoardExportBlobService(
    BlobServiceClient blobServiceClient,
    IOptions<BlobStorageOptions> options,
    ILogger<BoardExportBlobService> logger) : IBoardExportBlobService
{
    private const string _zipContentType = "application/zip";

    public async Task<Stream> DownloadAttachmentAsync(string blobName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        var blobOptions = options.Value;
        blobOptions.Validate();

        var response = await blobServiceClient
            .GetBlobContainerClient(blobOptions.TaskAttachmentsContainerName)
            .GetBlobClient(blobName)
            .DownloadStreamingAsync(cancellationToken: ct);

        return response.Value.Content;
    }

    public async Task UploadArchiveAsync(Guid boardId, BoardExportArchive archive, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(boardId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(archive);

        var blobOptions = options.Value;
        blobOptions.Validate();

        var blobName = $"{boardId:D}/{archive.FileName}";

        if (archive.Content.CanSeek)
        {
            archive.Content.Position = 0;
        }

        await blobServiceClient
            .GetBlobContainerClient(blobOptions.ArchivesContainerName)
            .GetBlobClient(blobName)
            .UploadAsync(
                archive.Content,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = _zipContentType },
                },
                ct);

        logger.LogInformation(
            "Board export archive uploaded. BoardId={BoardId}, Container={Container}, BlobName={BlobName}",
            boardId,
            blobOptions.ArchivesContainerName,
            blobName);
    }
}
