using System.Text.Json.Serialization;

namespace Contracts.DTOs;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BoardExportStatusDto : byte
{
    None = 0,
    Requested = 1,
    Pending = 2,
    Processing = 3,
    Completed = 4,
    Failed = 5
}
