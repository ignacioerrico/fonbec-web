namespace Fonbec.Web.Logic.Models.Students;

public enum LetterAggregateStatus
{
    NoPlan = 0,
    Exempt = 1,
    Rejected = 2,
    Pending = 3,
    Approved = 4,
}

public enum LetterSlotStatus
{
    None = 0,
    InReview = 1,
    Approved = 2,
    Rejected = 3,
}
