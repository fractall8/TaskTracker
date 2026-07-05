using System.Text.Json.Serialization;

namespace Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BoardRoleDto : byte
{
    User = 1,
    ScrumMaster = 2,
    Admin = 3
}
