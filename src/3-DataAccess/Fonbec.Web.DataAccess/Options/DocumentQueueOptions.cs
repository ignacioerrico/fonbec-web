namespace Fonbec.Web.DataAccess.Options;

public class DocumentQueueOptions
{
    public const string SectionName = "DocumentQueue";

    /// <summary>
    /// Minutes a document stays locked after being taken for review or digital improvement.
    /// If the holder neither approves/rejects (review) nor submits (improvement) — and does not
    /// explicitly release the lock — within this window, the lock <b>expires</b> and the document
    /// becomes takeable again by the next reviewer, even if later documents in the queue are still
    /// locked. Shared by both the review lock and the improvement lock. Default 40.
    /// </summary>
    public int ReviewLockTimeoutMinutes { get; set; } = 40;
}
