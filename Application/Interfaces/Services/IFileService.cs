namespace Application.Interfaces.Services;

public interface IFileService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    
    Task DeleteFileAsync(string fileName, CancellationToken cancellationToken = default);
}