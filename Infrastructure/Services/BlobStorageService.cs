using Application.Interfaces.Services;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Infrastructure.Services;

public class BlobStorageService(BlobServiceClient blobServiceClient) : IFileService
{
    private const string ContainerName = "files";
    
    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType,
        CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var uniqueFileName = $"{{Guid.NewGuid()}}_{fileName}";
        var blobClient = containerClient.GetBlobClient(uniqueFileName);

        var httpHeaders = new BlobHttpHeaders { ContentType = contentType };
        await blobClient.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = httpHeaders }, cancellationToken);

        return blobClient.Uri.ToString();
    }

    public Task DeleteFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        var blobClient = containerClient.GetBlobClient(fileName);
        return blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}