using System.IO.Compression;
using Contracts.DTOs;
using TaskTracker.Functions.Models;

namespace TaskTracker.Functions.Interfaces;

public interface IBoardExportSummaryWriter
{
    BoardExportSummaryFormat Format { get; }

    Task WriteAsync(ZipArchive archive, BoardExportDataDto data, CancellationToken ct = default);
}
