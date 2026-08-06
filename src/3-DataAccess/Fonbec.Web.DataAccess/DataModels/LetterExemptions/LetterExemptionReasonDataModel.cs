namespace Fonbec.Web.DataAccess.DataModels.LetterExemptions;

/// <summary>An active (non-revoked) letter exemption for a student in a plan, with its reason.</summary>
public class LetterExemptionReasonDataModel
{
    public int StudentId { get; set; }

    public string Reason { get; set; } = null!;
}