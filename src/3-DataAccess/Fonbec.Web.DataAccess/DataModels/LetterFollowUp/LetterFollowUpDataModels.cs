using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.LetterFollowUp;

public sealed class LetterFollowUpQueryResultDataModel
{
    public List<LetterFollowUpTaskDataModel> RedFlags { get; set; } = [];

    public List<LetterFollowUpTaskDataModel> GreenFlags { get; set; } = [];
}

public sealed class LetterFollowUpTaskDataModel
{
    public long AssessmentId { get; set; }

    public string StudentFirstName { get; set; } = string.Empty;

    public string StudentLastName { get; set; } = string.Empty;

    public string FacilitatorFirstName { get; set; } = string.Empty;

    public string FacilitatorLastName { get; set; } = string.Empty;

    public string FacilitatorEmail { get; set; } = string.Empty;

    public string ReviewerFirstName { get; set; } = string.Empty;

    public string ReviewerLastName { get; set; } = string.Empty;

    public string ReviewerEmail { get; set; } = string.Empty;

    public DateTime ReportedOn { get; set; }

    public string Comment { get; set; } = string.Empty;

    public RedFlagPriority? Priority { get; set; }
}