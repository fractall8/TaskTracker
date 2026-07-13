using System.IO.Compression;
using Contracts.DTOs;
using Microsoft.Extensions.Logging;
using TaskTracker.Functions.Interfaces;
using TaskTracker.Functions.Models;

namespace TaskTracker.Functions.Archiving;

public sealed class BoardArchiveBuilder(
    BoardExportSummaryWriterRegistry summaryWriterRegistry,
    IBoardExportBlobService exportBlobService,
    ILogger<BoardArchiveBuilder> logger) : IBoardArchiveBuilder
{
    public async Task<BoardExportArchive> BuildAsync(
        BoardExportDataDto data,
        IReadOnlyList<BoardExportSummaryFormat> summaryFormats,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(summaryFormats);

        if (summaryFormats.Count == 0)
        {
            throw new ArgumentException("At least one summary format is required.", nameof(summaryFormats));
        }

        var pathBuilder = new BoardArchivePathBuilder();
        var boardId = data.Board.Id;
        var fileName = pathBuilder.BuildArchiveFileName(data.Board.Name);
        var summaryData = data.AppliedOptions.IncludeAttachments
            ? EnrichWithArchiveRelativePaths(data, pathBuilder)
            : data;

        var tempFilePath = Path.GetTempFileName();
        var resultStream = new FileStream(
            tempFilePath,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.None,
            4096,
            FileOptions.Asynchronous | FileOptions.DeleteOnClose);

        await using (var archive = new ZipArchive(resultStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var format in summaryFormats)
            {
                var summaryWriter = summaryWriterRegistry.Get(format);
                await summaryWriter.WriteAsync(archive, summaryData, ct);
            }

            if (data.AppliedOptions.IncludeAttachments)
            {
                await WriteAttachmentsAsync(archive, summaryData, ct);
            }
        }

        resultStream.Position = 0;

        logger.LogInformation(
            "Board export archive built. BoardId={BoardId}, FileName={FileName}, SizeBytes={SizeBytes}",
            boardId,
            fileName,
            resultStream.Length);

        return new BoardExportArchive(resultStream, fileName);
    }

    private static BoardExportDataDto EnrichWithArchiveRelativePaths(
        BoardExportDataDto data,
        BoardArchivePathBuilder pathBuilder)
    {
        var columns = data.Columns
            .Select(column => column with
            {
                Tasks = column.Tasks
                    .Select(task => task with
                    {
                        Attachments = task.Attachments?
                            .Select(attachment => attachment with
                            {
                                ArchiveRelativePath = pathBuilder.BuildAttachmentEntryPath(
                                    task.Id,
                                    task.Title,
                                    attachment.OriginalFileName),
                            })
                            .ToList(),
                    })
                    .ToList(),
            })
            .ToList();

        return data with { Columns = columns };
    }

    private async Task WriteAttachmentsAsync(
        ZipArchive archive,
        BoardExportDataDto data,
        CancellationToken ct)
    {
        foreach (var column in data.Columns)
        {
            foreach (var task in column.Tasks)
            {
                if (task.Attachments is not { Count: > 0 })
                {
                    continue;
                }

                foreach (var attachment in task.Attachments.OrderBy(a => a.CreatedAt))
                {
                    ct.ThrowIfCancellationRequested();

                    if (attachment.ArchiveRelativePath is not { Length: > 0 } entryPath)
                    {
                        throw new InvalidOperationException(
                            $"Archive relative path is missing for attachment {attachment.Id}.");
                    }

                    var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);

                    try
                    {
                        await using var attachmentStream = await exportBlobService.DownloadAttachmentAsync(
                            attachment.BlobName,
                            ct);
                        await using var entryStream = entry.Open();
                        await attachmentStream.CopyToAsync(entryStream, ct);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "Failed to download attachment {AttachmentId} ('{FileName}') for task {TaskId}. BoardId={BoardId}",
                            attachment.Id,
                            attachment.OriginalFileName,
                            task.Id,
                            data.Board.Id);

                        throw;
                    }
                }
            }
        }
    }
}
