using Application.Interfaces.Services;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Infrastructure.Services;

public class BlobStorageService(BlobServiceClient blobServiceClient) : IFileService
{
    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType,
        string containerName, CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var blobClient = containerClient.GetBlobClient(uniqueFileName);

        var httpHeaders = new BlobHttpHeaders { ContentType = contentType };
        await blobClient.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = httpHeaders },
            cancellationToken);

        var fileUrl = blobClient.Uri.ToString();

        // This is only for local Docker development (Azurite). 
        // In production, the URL won't contain "azurite", so this replace is safely ignored.
        return fileUrl.Replace("azurite", "localhost");
    }

    public Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        var blobUriBuilder = new BlobUriBuilder(new Uri(fileUrl));
        
        var containerClient = blobServiceClient.GetBlobContainerClient(blobUriBuilder.BlobContainerName);
        var blobClient = containerClient.GetBlobClient(blobUriBuilder.BlobName);
        
        return blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}