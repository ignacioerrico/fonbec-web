namespace Fonbec.Web.DataAccess.Entities.Enums;

public enum DocumentStatus : byte
{
    Pending = 0,
    PendingImprovement = 1,
    ProcessingImprovement = 2,
    Processing = 3,
    Approved = 4,
    Rejected = 5,
}