using FluentAssertions;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class DocumentNotificationQueueTests
{
    [Fact]
    public async Task EnqueueSponsorNotification_Returns_Before_Send_Finishes()
    {
        var notifications = Substitute.For<IDocumentNotificationService>();
        var repository = Substitute.For<IDocumentRepository>();
        repository.GetDocumentIdsWithUnnotifiedSharesAsync().Returns([]);

        var sendStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSend = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        notifications.NotifySponsorsAsync(42, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                sendStarted.TrySetResult();
                return releaseSend.Task;
            });

        var queue = CreateQueue(notifications, repository);
        await queue.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await queue.EnqueueSponsorNotificationAsync(42);
            await sendStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            releaseSend.Task.IsCompleted.Should().BeFalse();
        }
        finally
        {
            releaseSend.TrySetResult();
            await queue.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Start_Requeues_Documents_With_Unnotified_Shares()
    {
        var notifications = Substitute.For<IDocumentNotificationService>();
        var repository = Substitute.For<IDocumentRepository>();
        repository.GetDocumentIdsWithUnnotifiedSharesAsync().Returns([7L]);

        var sent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        notifications.NotifySponsorsAsync(7, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                sent.TrySetResult();
                return Task.CompletedTask;
            });

        var queue = CreateQueue(notifications, repository);
        await queue.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await sent.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            await notifications.Received(1).NotifySponsorsAsync(7, Arg.Any<CancellationToken>());
        }
        finally
        {
            await queue.StopAsync(CancellationToken.None);
        }
    }

    private static DocumentNotificationQueue CreateQueue(
        IDocumentNotificationService notifications,
        IDocumentRepository repository)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDocumentNotificationService>(notifications);
        services.AddSingleton<IDocumentRepository>(repository);
        var provider = services.BuildServiceProvider();

        return new DocumentNotificationQueue(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<DocumentNotificationQueue>.Instance);
    }
}