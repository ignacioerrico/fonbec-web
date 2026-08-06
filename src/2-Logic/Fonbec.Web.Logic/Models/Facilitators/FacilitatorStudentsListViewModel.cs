using System.Globalization;
using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Students;
using Mapster;

namespace Fonbec.Web.Logic.Models.Facilitators;

public class StudentsDashboardViewModel
{
    public int? CurrentPlanId { get; set; }

    public DateTime? CurrentPlanStartsOn { get; set; }

    public List<FacilitatorStudentsListViewModel> Students { get; set; } = [];

    /// <summary>
    /// The current plan period rendered for the "Carta" column header, e.g. "junio de 2026".
    /// Null when the facilitator's chapter has no active plan.
    /// </summary>
    public string? CurrentPlanLabel
    {
        get
        {
            if (CurrentPlanStartsOn is not { } startsOn)
            {
                return null;
            }

            return CurrentPlanStartsOn.Value.ToString(@"MMMM \d\e yyyy", new CultureInfo("es-AR"));
        }
    }
}

public class FacilitatorStudentsListViewModel : AuditableViewModel, IDetectChanges<FacilitatorStudentsListViewModel>
{
    public int StudentId { get; set; }

    public string StudentFirstName { get; set; } = null!;

    public string StudentLastName { get; set; } = null!;

    public string? StudentNickName { get; set; }

    public EducationLevel EducationLevel { get; set; }

    /// <summary>
    /// True when the student is exempt from submitting a letter for the current plan (us110).
    /// When set, the "Subir carta" action is unavailable.
    /// </summary>
    public bool IsLetterExemptForCurrentPlan { get; set; }

    /// <summary>Reason for the current-plan letter exemption (us110), when the student is exempt.</summary>
    public string? LetterExemptionReason { get; set; }

    public LetterAggregateStatus LetterAggregate { get; set; }
    public List<DashboardSponsorViewModel> Sponsors { get; set; } = [];
    public List<SponsorLetterStatusViewModel> LetterStatuses { get; set; } = [];

    /// <summary>Total number of letter slots (one per active sponsorship).</summary>
    public int SponsorLetterCount => LetterStatuses.Count;

    /// <summary>Number of sponsors whose letter is already uploaded (in review or approved).</summary>
    public int UploadedLetterCount => LetterStatuses.Count(s => LetterAggregation.IsSatisfied(s.Status));

    /// <summary>
    /// True when the aggregate badge (uploaded/total) should be shown: more than one sponsor and an
    /// upload-based aggregate state (exempt / no-plan have no meaningful count).
    /// </summary>
    public bool ShowLetterCountBadge =>
        LetterStatuses.Count > 1
        && LetterAggregate is LetterAggregateStatus.NotUploaded
            or LetterAggregateStatus.Partial
            or LetterAggregateStatus.Complete;

    /// <summary>Sponsors whose letter is missing or was rejected, i.e. the ones that still need a (re)upload.</summary>
    public IReadOnlyList<SponsorLetterStatusViewModel> SponsorsNeedingLetter =>
        LetterStatuses.Where(s => LetterAggregation.NeedsUpload(s.Status)).ToList();

    public bool IsIdenticalTo(FacilitatorStudentsListViewModel other) =>
        StudentFirstName == other.StudentFirstName.NormalizeText()
        && StudentLastName == other.StudentLastName.NormalizeText()
        && (StudentNickName?.NormalizeText() ?? string.Empty) == (other.StudentNickName?.NormalizeText() ?? string.Empty)
        && EducationLevel == other.EducationLevel
        && Sponsors.Count == other.Sponsors.Count
        && Sponsors.All(s => other.Sponsors.Any(os =>
            os.SponsorshipId == s.SponsorshipId
            && os.SponsorId == s.SponsorId
            && os.CompanyId == s.CompanyId
            && os.IsCompany == s.IsCompany
            && os.RecipientName == s.RecipientName));
}

public class DashboardSponsorViewModel
{
    public int SponsorshipId { get; set; }

    public int? SponsorId { get; set; }

    public int? CompanyId { get; set; }

    public string RecipientName { get; set; } = null!;

    public bool IsCompany { get; set; }
}

public class FacilitatorStudentsListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<FacilitatorStudentsDataModel, FacilitatorStudentsListViewModel>()
            .Map(dest => dest.StudentId, src => src.StudentId)
            .Map(dest => dest.StudentFirstName, src => src.StudentFirstName)
            .Map(dest => dest.StudentLastName, src => src.StudentLastName)
            .Map(dest => dest.StudentNickName, src => src.StudentNickName)
            .Map(dest => dest.EducationLevel, src => src.EducationLevel)
            .Map(dest => dest.Sponsors, src => src.Sponsors);

        config.NewConfig<DashboardSponsorDataModel, DashboardSponsorViewModel>()
            .Map(dest => dest.SponsorshipId, src => src.SponsorshipId)
            .Map(dest => dest.SponsorId, src => src.SponsorId)
            .Map(dest => dest.CompanyId, src => src.CompanyId)
            .Map(dest => dest.RecipientName, src => src.RecipientName)
            .Map(dest => dest.IsCompany, src => src.IsCompany);
    }
}