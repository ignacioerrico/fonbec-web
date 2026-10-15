namespace Fonbec.Web.DataAccess.Entities.Enums;

public enum DocumentStatus : byte
{
    DigitalImprovementPending = 0,
    DigitalImprovementOngoing = 1,
    ReviewPending = 2,
    ReviewOngoing = 3,
    Approved = 4,
    Rejected = 5,

    /// <summary>
    /// A reviewer rejected the document, and the chapter holds it until a manager
    /// shares it with the sponsor or returns it to the facilitator.
    /// </summary>
    PendingManagerDecision = 6,
}