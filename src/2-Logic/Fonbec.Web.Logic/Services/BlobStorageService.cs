using System.Security.Cryptography;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Fonbec.Web.Logic.Models.Documents;
using Fonbec.Web.Logic.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fonbec.Web.Logic.Services;

public interface IBlobStorageService
{
    Task<UploadBlobResult> UploadAsync(
        Stream content,
        string blobName,
        string mimeType,
        CancellationToken cancellationToken = default);

    Task<DownloadBlobResult?> DownloadAsync(
        string blobName,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string blobName,
        CancellationToken cancellationToken = default);

    Task EnsureContainerExistsAsync(CancellationToken cancellationToken = default);
}

public class BlobStorageService(
    BlobServiceClient blobServiceClient,
    IOptions<BlobStorageOptions> options,
    ILogger<BlobStorageService> logger) : IBlobStorageService
{
    private readonly BlobStorageOptions _options = options.Value;

    public async Task EnsureContainerExistsAsync(CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<UploadBlobResult> UploadAsync(
        Stream content,
        string blobName,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        // Buffer the content so we can compute the SHA-256 hash and length before uploading.
        // Files are small (bounded by BlobStorageOptions.MaxFileSizeBytes).
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        var sha256 = SHA256.HashData(bytes);

        var blobClient = container.GetBlobClient(blobName);
        using var uploadStream = new MemoryStream(bytes, writable: false);
        await blobClient.UploadAsync(
            uploadStream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = mimeType },
            },
            cancellationToken);

        return new UploadBlobResult
        {
            BlobName = blobName,
            MimeType = mimeType,
            FileSizeBytes = bytes.LongLength,
            Sha256 = sha256,
        };
    }

    public async Task<DownloadBlobResult?> DownloadAsync(
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        var blobClient = container.GetBlobClient(blobName);

        try
        {
            var download = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return new DownloadBlobResult
            {
                Content = download.Value.Content,
                MimeType = download.Value.Details.ContentType,
                FileSizeBytes = download.Value.Details.ContentLength,
            };
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            logger.LogWarning("Blob '{BlobName}' was not found in container '{Container}'.",
                blobName, _options.ContainerName);
            return null;
        }
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        var blobClient = container.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}