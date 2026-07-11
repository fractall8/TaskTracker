namespace Contracts.DTOs;

public record BoardExportAttachmentDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    int Position,
    DateTimeOffset CreatedAt,
    BoardExportUserDto UploadedBy,
    string BlobName);
