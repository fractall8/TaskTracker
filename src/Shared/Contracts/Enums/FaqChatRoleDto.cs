using System.Text.Json.Serialization;

namespace Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FaqChatRoleDto : byte
{
    User = 1,
    Assistant = 2
}
