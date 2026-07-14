namespace Application.Interfaces.Services;

public interface IFileService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string containerName,
        bool isPublic = false, CancellationToken cancellationToken = default);

    Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);

    Task<(bool Exists, string? DownloadUrl, string? FileName)> GetDownloadUrlByPrefixAsync(
        string containerName,
        string prefix,
        TimeSpan? expiry = null,
        CancellationToken ct = default);
}
