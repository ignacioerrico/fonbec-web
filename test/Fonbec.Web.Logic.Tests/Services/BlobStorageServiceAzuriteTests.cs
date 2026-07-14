using System.Security.Cryptography;
using System.Text;
using Azure.Storage.Blobs;
using FluentAssertions;
using Fonbec.Web.Logic.Options;
using Fonbec.Web.Logic.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fonbec.Web.Logic.Tests.Services;

/// <summary>
/// Integration test that runs against a local Azurite storage emulator
/// (connection string <c>UseDevelopmentStorage=true</c>). The test is skipped
/// automatically when Azurite is not reachable, so it never fails CI environments
/// that do not run the emulator.
///
/// To run it locally, start Azurite (for example: <c>azurite --silent</c>).
/// </summary>
public class BlobStorageServiceAzuriteTests
{
    private const string DevelopmentConnectionString = "UseDevelopmentStorage=true";

    [Fact]
    public async Task UploadDownloadDelete_RoundTripsThroughAzurite()
    {
        var containerName = "us100-" + Guid.NewGuid().ToString("N");
        var serviceClient = new BlobServiceClient(DevelopmentConnectionString);
        var service = new BlobStorageService(
            serviceClient,
            Microsoft.Extensions.Options.Options.Create(new BlobStorageOptions { ContainerName = containerName }),
            NullLogger<BlobStorageService>.Instance);

        try
        {
            await service.EnsureContainerExistsAsync(TestContext.Current.CancellationToken);
        }
        catch (Exception ex)
        {
            Assert.Skip($"Azurite is not available for the integration test: {ex.Message}");
            return;
        }

        try
        {
            const string blobName = "3/5/report-card/original/sample.txt";
            var payload = Encoding.UTF8.GetBytes("azurite round trip");

            var upload = await service.UploadAsync(
                new MemoryStream(payload), blobName, "text/plain", TestContext.Current.CancellationToken);

            upload.BlobName.Should().Be(blobName);
            upload.FileSizeBytes.Should().Be(payload.LongLength);
            upload.Sha256.Should().Equal(SHA256.HashData(payload));

            var download = await service.DownloadAsync(blobName, TestContext.Current.CancellationToken);
            download.Should().NotBeNull();
            download!.MimeType.Should().Be("text/plain");
            using (var reader = new StreamReader(download.Content, Encoding.UTF8))
            {
                (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).Should().Be("azurite round trip");
            }

            await service.DeleteAsync(blobName, TestContext.Current.CancellationToken);
            (await service.DownloadAsync(blobName, TestContext.Current.CancellationToken)).Should().BeNull();
        }
        finally
        {
            try
            {
                await serviceClient.GetBlobContainerClient(containerName)
                    .DeleteIfExistsAsync(cancellationToken: TestContext.Current.CancellationToken);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }
}