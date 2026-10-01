namespace Fonbec.Web.Logic.Services;

/// <summary>
/// Accepts notification work and returns without sending mail. A background worker performs the send.
/// </summary>
public interface IDocumentNotificationQueue
{
    Task EnqueueSponsorNotificationAsync(long documentId);

    Task EnqueuePlanReadyNotificationAsync(int chapterId, int planId, DateTime planStartsOn);
}