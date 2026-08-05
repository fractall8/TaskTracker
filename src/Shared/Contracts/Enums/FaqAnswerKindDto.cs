using System.Text.Json.Serialization;

namespace Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FaqAnswerKindDto : byte
{
    Grounded = 1,

    // Answered from the conversation itself; no product facts, no citations.
    Conversational = 2,

    // Nothing in the documentation covered it — must not be styled as authoritative.
    Unsupported = 3,

    // Answered from the caller's own workspace data via tools; no documentation citations.
    DataBacked = 4
}
