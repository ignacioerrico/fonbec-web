using System.Security.Cryptography;
using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using Fonbec.Web.Logic.Options;
using Fonbec.Web.Logic.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class BlobStorageServiceTests
{
    private const string ContainerName = "documents";

    private readonly BlobServiceClient _serviceClient = Substitute.For<BlobServiceClient>();
    private readonly BlobContainerClient _containerClient = Substitute.For<BlobContainerClient>();
    private readonly BlobClient _blobClient = Substitute.For<BlobClient>();

    public BlobStorageServiceTests()
    {
        _serviceClient.GetBlobContainerClient(ContainerName).Returns(_containerClient);
        _containerClient.GetBlobClient(Arg.Any<string>()).Returns(_blobClient);
    }

    private BlobStorageService CreateService() =>
        new(_serviceClient,
            Microsoft.Extensions.Options.Options.Create(new BlobStorageOptions { ContainerName = ContainerName }),
            NullLogger<BlobStorageService>.Instance);

    [Fact]
    public async Task UploadAsync_ComputesSha256AndSize_AndSetsContentType()
    {
        var payload = Encoding.UTF8.GetBytes("hello world");
        byte[]? uploadedBytes = null;
        BlobUploadOptions? uploadedOptions = null;

        _blobClient
            .UploadAsync(
                Arg.Do<Stream>(s =>
                {
                    using var ms = new MemoryStream();
                    s.CopyTo(ms);
                    uploadedBytes = ms.ToArray();
                }),
                Arg.Do<BlobUploadOptions>(o => uploadedOptions = o),
                Arg.Any<CancellationToken>())
            .Returns(Substitute.For<Response<BlobContentInfo>>());

        var service = CreateService();

        var result = await service.UploadAsync(new MemoryStream(payload), "1/2/original/file.pdf", "application/pdf", TestContext.Current.CancellationToken);

        result.BlobName.Should().Be("1/2/original/file.pdf");
        result.MimeType.Should().Be("application/pdf");
        result.FileSizeBytes.Should().Be(payload.LongLength);
        result.Sha256.Should().Equal(SHA256.HashData(payload));

        uploadedBytes.Should().Equal(payload);
        uploadedOptions!.HttpHeaders!.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task UploadAsync_CreatesContainerIfMissing()
    {
        _blobClient
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<BlobUploadOptions>(), Arg.Any<CancellationToken>())
            .Returns(Substitute.For<Response<BlobContentInfo>>());

        var service = CreateService();

        await service.UploadAsync(new MemoryStream([1, 2, 3]), "blob.txt", "text/plain", TestContext.Current.CancellationToken);

        await _containerClient.Received().CreateIfNotExistsAsync(
            Arg.Any<PublicAccessType>(),
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<BlobContainerEncryptionScopeOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureContainerExistsAsync_CreatesContainer()
    {
        var service = CreateService();

        await service.EnsureContainerExistsAsync(TestContext.Current.CancellationToken);

        await _containerClient.Received().CreateIfNotExistsAsync(
            Arg.Any<PublicAccessType>(),
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<BlobContainerEncryptionScopeOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_CallsDeleteIfExists()
    {
        var service = CreateService();

        await service.DeleteAsync("some/blob.pdf", TestContext.Current.CancellationToken);

        await _blobClient.Received().DeleteIfExistsAsync(
            Arg.Any<DeleteSnapshotsOption>(),
            Arg.Any<BlobRequestConditions>(),
            Arg.Any<CancellationToken>());
    }
}