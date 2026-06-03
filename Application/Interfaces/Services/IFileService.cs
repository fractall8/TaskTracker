using Microsoft.AspNetCore.Http;

namespace Application.Interfaces.Services;

public interface IFileService
{
    Task<string> UploadFileAsync(IFormFile formFile, string containerName, CancellationToken cancellationToken = default);
    
    Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
}