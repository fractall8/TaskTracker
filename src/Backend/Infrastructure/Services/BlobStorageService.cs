using Application.Interfaces.Services;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Infrastructure.Services;

public class BlobStorageService(BlobServiceClient blobServiceClient) : IFileService
{
    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType,
        string containerName, bool isPublic = false, CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        var accessType = isPublic ? PublicAccessType.Blob : PublicAccessType.None;

        await containerClient.CreateIfNotExistsAsync(accessType, cancellationToken: cancellationToken);

        var extension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var blobClient = containerClient.GetBlobClient(uniqueFileName);
        var httpHeaders = new BlobHttpHeaders { ContentType = contentType };

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = httpHeaders,
            TransferOptions = new StorageTransferOptions
            {
                MaximumTransferSize = 2 * 1024 * 1024,
                InitialTransferSize = 2 * 1024 * 1024,
                MaximumConcurrency = 3
            }
        };

        await blobClient.UploadAsync(fileStream, uploadOptions, cancellationToken);

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
