using Fonbec.Web.DataAccess.Entities.Abstract;

namespace Fonbec.Web.DataAccess.DataModels.Students;

public class FacilitatorStudentsDataModel(Auditable auditable) : AuditableDataModel(auditable)
{
    public int StudentId { get; set; }

    public string StudentFirstName { get; set; } = null!;

    public string StudentLastName { get; set; } = null!;
}

