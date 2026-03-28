using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models;

namespace Fonbec.Web.Ui.Models.Sponsorship;

public class SponsorshipCreateBindModel
{
    // This indicates which value is not null of SelectedSponsor and SelectedCompanyId
    public SponsorshipType SponsorshipType { get; set; }

    public SelectableModel<int>? SelectedSponsor { get; set; }

    public int? SelectedCompanyId { get; set; }

    // Normally this wouldn't be nullable, but MudDatePicker requires it to be nullable.
    public DateTime? SponsorshipStartDate { get; set; }

    public DateTime? SponsorshipEndDate { get; set; }

    public string SponsorshipNotes { get; set; } = string.Empty;

    public int CreatedById { get; set; }
}