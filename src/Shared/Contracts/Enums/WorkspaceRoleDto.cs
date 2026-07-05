using System.Text.Json.Serialization;

namespace Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkspaceRoleDto : byte
{
    Member = 1,
    Admin = 2,
    Owner = 3
}
