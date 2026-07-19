using System.Collections.Concurrent;
using Application.Interfaces.Services;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace Infrastructure.Services;

public class BlobStorageService(BlobServiceClient blobServiceClient) : IFileService
{
    private readonly ConcurrentDictionary<string, BlobContainerClient> _containerClients = new(StringComparer.Ordinal);

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

    public async Task<(bool Exists, string? DownloadUrl, string? FileName)> GetDownloadUrlByPrefixAsync(
        string containerName,
        string prefix,
        TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        var containerClient = GetContainerClient(containerName);

        try
        {
            var blobs = containerClient.GetBlobsAsync(options: new GetBlobsOptions { Prefix = prefix }, cancellationToken: ct);
            var enumerator = blobs.GetAsyncEnumerator(ct);

            if (!await enumerator.MoveNextAsync())
            {
                return (false, null, null);
            }

            var blobItem = enumerator.Current;
            var blobClient = containerClient.GetBlobClient(blobItem.Name);

            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException("BlobClient cannot generate SAS URI. Ensure StorageSharedKeyCredential is provided.");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobItem.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiry ?? TimeSpan.FromMinutes(5))
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            var downloadUrl = sasUri.ToString().Replace("azurite", "localhost");

            var actualFileName = Path.GetFileName(blobItem.Name);

            return (true, downloadUrl, actualFileName);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ContainerNotFound")
        {
            return (false, null, null);
        }
    }

    public Task<string> GetDownloadUrlAsync(string fileUrl, string originalFileName, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var parsedUrl = fileUrl.Contains("localhost") ? fileUrl.Replace("localhost", "azurite") : fileUrl;
        var blobUriBuilder = new BlobUriBuilder(new Uri(parsedUrl));

        var containerClient = GetContainerClient(blobUriBuilder.BlobContainerName);
        var blobClient = containerClient.GetBlobClient(blobUriBuilder.BlobName);

        if (!blobClient.CanGenerateSasUri)
        {
            throw new InvalidOperationException("BlobClient cannot generate SAS URI. Ensure StorageSharedKeyCredential is provided.");
        }

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = blobUriBuilder.BlobContainerName,
            BlobName = blobUriBuilder.BlobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry ?? TimeSpan.FromMinutes(5))
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        sasBuilder.ContentDisposition = $"attachment; filename*=UTF-8''{Uri.EscapeDataString(originalFileName)}";

        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        var downloadUrl = sasUri.ToString().Replace("azurite", "localhost");

        return Task.FromResult(downloadUrl);
    }

    private BlobClient GetBlobClient(string containerName, string blobName) =>
        GetContainerClient(containerName).GetBlobClient(blobName);

    private BlobContainerClient GetContainerClient(string containerName) =>
        _containerClients.GetOrAdd(
            containerName,
            blobServiceClient.GetBlobContainerClient);

}
