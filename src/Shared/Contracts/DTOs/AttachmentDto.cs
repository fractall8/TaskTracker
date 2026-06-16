namespace Contracts.DTOs;

public record AttachmentDto(
    Guid Id,
    string FileName,
    string FileUrl,
    long SizeInBytes,
    DateTimeOffset CreatedAt,
    Guid? CreatedById);