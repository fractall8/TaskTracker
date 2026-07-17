namespace Contracts.DTOs;

public record BoardArchivedDto(
    DateTimeOffset ArchivedAt,
    BoardExportStatusDto BoardExportStatus);
