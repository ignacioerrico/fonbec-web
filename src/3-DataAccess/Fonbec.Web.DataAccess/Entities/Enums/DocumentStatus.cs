namespace Fonbec.Web.DataAccess.Entities.Enums;

public enum DocumentStatus : byte
{
    DigitalImprovementPending = 0,
    DigitalImprovementOngoing = 1,
    ReviewPending = 2,
    ReviewOngoing = 3,
    Approved = 4,
    Rejected = 5,
}