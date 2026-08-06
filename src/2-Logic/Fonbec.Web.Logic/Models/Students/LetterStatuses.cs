namespace Fonbec.Web.Logic.Models.Students;

public enum LetterAggregateStatus
{
    /// <summary>No active plan for the chapter: nothing to upload.</summary>
    NoPlan = 0,

    /// <summary>Student is exempt from letters for the current plan (us110).</summary>
    Exempt = 1,

    /// <summary>No letter has been uploaded for any sponsor (or every letter still needs (re)uploading).</summary>
    NotUploaded = 2,

    /// <summary>At least one letter is uploaded, but at least one sponsor still needs one.</summary>
    Partial = 3,

    /// <summary>Every sponsor has an uploaded letter (in review or approved).</summary>
    Complete = 4,
}

public enum LetterSlotStatus
{
    None = 0,
    InReview = 1,
    Approved = 2,
    Rejected = 3,
}