using System.Text.Json.Serialization;

namespace Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FaqAnswerKindDto : byte
{
    /// <summary>Answered from retrieved documentation; <c>Citations</c> is populated.</summary>
    Grounded = 1,

    /// <summary>
    /// Answered from the conversation itself — a greeting, a name, or a question about what was already
    /// said. Carries no product facts and no citations.
    /// </summary>
    Conversational = 2,

    /// <summary>Nothing in the documentation covered the question. Must not look authoritative.</summary>
    Unsupported = 3
}
