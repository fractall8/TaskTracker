using Contracts.DTOs;
using Newtonsoft.Json;

namespace Contracts.Export;

// partition by /boardId
public sealed class BoardExportDocument
{
    public const string IdJson = "id";
    public const string BoardIdJson = "boardId";
    public const string ExportStatusJson = "exportStatus";
    public const string ExportStatusNameJson = "exportStatusName";
    public const string UpdatedAtUtcJson = "updatedAtUtc";
    public const string ErrorMessageJson = "errorMessage";
    public const string ExportOptionsJson = "exportOptions";
    public const string ReExportStatusJson = "reExportStatus";
    public const string ReExportStatusNameJson = "reExportStatusName";
    public const string ReExportOptionsJson = "reExportOptions";

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
