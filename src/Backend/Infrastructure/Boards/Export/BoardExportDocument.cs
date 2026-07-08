using Contracts.DTOs;
using Newtonsoft.Json;

namespace Infrastructure.Boards.Export;

internal sealed class BoardExportDocument
{
    internal const string IdJson = "id";
    internal const string BoardIdJson = "boardId";
    internal const string ExportStatusJson = "exportStatus";
    internal const string ExportStatusNameJson = "exportStatusName";
    internal const string UpdatedAtUtcJson = "updatedAtUtc";
    internal const string ErrorMessageJson = "errorMessage";
    internal const string ExportOptionsJson = "exportOptions";
    internal const string ReExportStatusJson = "reExportStatus";
    internal const string ReExportStatusNameJson = "reExportStatusName";
    internal const string ReExportOptionsJson = "reExportOptions";

    [JsonProperty(IdJson)]
    public required string Id { get; init; }

    [JsonProperty(BoardIdJson)]
    public required Guid BoardId { get; init; }

    [JsonProperty(ExportStatusJson)]
    public required int ExportStatus { get; init; }

    [JsonProperty(ExportStatusNameJson)]
    public required string ExportStatusName { get; init; }

    [JsonProperty(UpdatedAtUtcJson)]
    public required DateTimeOffset UpdatedAtUtc { get; init; }

    [JsonProperty(ErrorMessageJson)]
    public string? ErrorMessage { get; init; }

    [JsonProperty(ExportOptionsJson)]
    public BoardExportOptionsDto? ExportOptions { get; init; }

    [JsonProperty(ReExportStatusJson)]
    public int? ReExportStatus { get; init; }

    [JsonProperty(ReExportStatusNameJson)]
    public string? ReExportStatusName { get; init; }

    [JsonProperty(ReExportOptionsJson)]
    public BoardExportOptionsDto? ReExportOptions { get; init; }
}
