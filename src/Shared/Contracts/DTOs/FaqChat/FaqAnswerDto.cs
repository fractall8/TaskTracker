using Contracts.Enums;

namespace Contracts.DTOs;

/// <summary>
/// <paramref name="Kind"/> drives how the UI presents the reply: a grounded answer, a friendly social
/// reply, or a refusal that must not be styled as authoritative.
/// </summary>
public record FaqAnswerDto(string Answer, FaqAnswerKindDto Kind, IReadOnlyList<FaqCitationDto> Citations);
