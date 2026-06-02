namespace Application.Interfaces.Services;

public interface IFileService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string containerName, CancellationToken cancellationToken = default);
    
    Task DeleteFileAsync(string fileName, string containerName, CancellationToken cancellationToken = default);
}