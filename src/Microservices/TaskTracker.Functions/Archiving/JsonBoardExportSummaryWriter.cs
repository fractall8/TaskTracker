using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Contracts.DTOs;
using TaskTracker.Functions.Interfaces;
using TaskTracker.Functions.Models;

namespace TaskTracker.Functions.Archiving;

public sealed class JsonBoardExportSummaryWriter : IBoardExportSummaryWriter
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public BoardExportSummaryFormat Format => BoardExportSummaryFormat.Json;

    public async Task WriteAsync(ZipArchive archive, BoardExportDataDto data, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentNullException.ThrowIfNull(data);

        var entry = archive.CreateEntry(BoardArchiveEntryNames.SummaryJson, CompressionLevel.Optimal);

        await using var entryStream = await entry.OpenAsync(ct);
        await JsonSerializer.SerializeAsync(entryStream, data, _jsonOptions, ct);
    }
}
