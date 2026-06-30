namespace Fonbec.Web.DataAccess.Entities;

public class DocumentQueueItem
{
    public long QueueItemId { get; set; }

    public long DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public DateTime EnqueuedAt { get; set; }

    public int Priority { get; set; }

    public int? ReviewLockedById { get; set; }
    public FonbecWebUser? ReviewLockedBy { get; set; }

    public DateTime? ReviewLockedAt { get; set; }

    public int DequeueCount { get; set; }
}