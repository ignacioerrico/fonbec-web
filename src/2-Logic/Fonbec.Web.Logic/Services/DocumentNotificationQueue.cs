using System.Threading.Channels;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fonbec.Web.Logic.Services;

/// <summary>
/// Sends document notifications off the request that approved or returned a document.
/// Sponsor shares, facilitator rejection emails, and manager decision emails left unmarked
/// are picked up again the next time the process starts.
/// </summary>
public sealed class DocumentNotificationQueue(
    IServiceScopeFactory scopeFactory,
    ILogger<DocumentNotificationQueue> logger) : BackgroundService, IDocumentNotificationQueue
{
    private readonly Channel<NotificationWork> _channel = Channel.CreateUnbounded<NotificationWork>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

    public Task EnqueueSponsorNotificationAsync(long documentId)
    {
        if (!_channel.Writer.TryWrite(new SponsorNotificationWork(documentId)))
        {
            logger.LogError(
                "Failed to queue sponsor notification for document {DocumentId}.",
                documentId);
        }

        return Task.CompletedTask;
    }

    public Task EnqueuePlanReadyNotificationAsync(int chapterId, int planId, DateTime planStartsOn)
    {
        if (!_channel.Writer.TryWrite(new PlanReadyNotificationWork(chapterId, planId, planStartsOn)))
        {
            logger.LogError(
                "Failed to queue plan-ready notification for plan {PlanId} in chapter {ChapterId}.",
                planId,
                chapterId);
        }

        return Task.CompletedTask;
    }

    public Task EnqueueFacilitatorRejectionNotificationAsync(long documentId)
    {
        if (!_channel.Writer.TryWrite(new FacilitatorRejectionNotificationWork(documentId)))
        {
            logger.LogError(
                "Failed to queue facilitator rejection notification for document {DocumentId}.",
                documentId);
        }

        return Task.CompletedTask;
    }

    public Task EnqueueHeldRejectionManagerNotificationAsync(long documentId)
    {
        if (!_channel.Writer.TryWrite(new HeldRejectionManagerNotificationWork(documentId)))
        {
            logger.LogError(
                "Failed to queue manager notification for held rejection {DocumentId}.",
                documentId);
        }

        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverUnnotifiedNotificationsAsync(stoppingToken);

        await foreach (var work in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            await DispatchAsync(work, stoppingToken);
        }
    }

    private async Task RecoverUnnotifiedNotificationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
            var sharedDocumentIds = await repository.GetDocumentIdsWithUnnotifiedSharesAsync() ?? [];
            foreach (var documentId in sharedDocumentIds)
            {
                await EnqueueSponsorNotificationAsync(documentId);
            }

            var rejectedDocumentIds = await repository.GetDocumentIdsWithUnnotifiedFacilitatorRejectionsAsync() ?? [];
            foreach (var documentId in rejectedDocumentIds)
            {
                await EnqueueFacilitatorRejectionNotificationAsync(documentId);
            }

            var heldDocumentIds = await repository.GetDocumentIdsWithUnnotifiedHeldRejectionsAsync() ?? [];
            foreach (var documentId in heldDocumentIds)
            {
                await EnqueueHeldRejectionManagerNotificationAsync(documentId);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to recover unnotified document notifications.");
        }
    }

    private async Task DispatchAsync(NotificationWork work, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var notifications = scope.ServiceProvider.GetRequiredService<IDocumentNotificationService>();

            switch (work)
            {
                case SponsorNotificationWork sponsor:
                    await notifications.NotifySponsorsAsync(sponsor.DocumentId, cancellationToken);
                    break;
                case PlanReadyNotificationWork plan:
                    await notifications.NotifyChapterManagersPlanReadyAsync(
                        plan.ChapterId,
                        plan.PlanId,
                        plan.PlanStartsOn,
                        cancellationToken);
                    break;
                case FacilitatorRejectionNotificationWork rejection:
                    await notifications.NotifyFacilitatorOfDocumentRejectionAsync(
                        rejection.DocumentId,
                        cancellationToken);
                    break;
                case HeldRejectionManagerNotificationWork held:
                    await notifications.NotifyManagersOfHeldRejectionAsync(
                        held.DocumentId,
                        cancellationToken);
                    break;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Document notification failed for {Work}.", work);
        }
    }

    private abstract record NotificationWork;

    private sealed record SponsorNotificationWork(long DocumentId) : NotificationWork;

    private sealed record PlanReadyNotificationWork(int ChapterId, int PlanId, DateTime PlanStartsOn)
        : NotificationWork;

    private sealed record FacilitatorRejectionNotificationWork(long DocumentId) : NotificationWork;

    private sealed record HeldRejectionManagerNotificationWork(long DocumentId) : NotificationWork;
}