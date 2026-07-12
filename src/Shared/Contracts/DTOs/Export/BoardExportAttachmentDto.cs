namespace Contracts.DTOs;

public record BoardExportAttachmentDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    DateTimeOffset CreatedAt,
    BoardExportUserDto UploadedBy,
    string BlobName,
    string? ArchiveRelativePath = null);
