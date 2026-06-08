namespace Contracts.DTOs;

public record BoardPreviewDto(
    Guid Id,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt
);