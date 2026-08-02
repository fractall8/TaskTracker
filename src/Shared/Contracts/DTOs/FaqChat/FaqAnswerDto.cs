namespace Contracts.DTOs;

/// <summary>
/// <paramref name="IsGrounded"/> is false when the knowledge base had nothing relevant, so the UI can
/// render a refusal differently from a real answer.
/// </summary>
public record FaqAnswerDto(string Answer, bool IsGrounded, IReadOnlyList<FaqCitationDto> Citations);
