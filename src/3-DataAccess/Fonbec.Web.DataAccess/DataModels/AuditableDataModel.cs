using Fonbec.Web.DataAccess.Entities;

namespace Fonbec.Web.DataAccess.DataModels;

public abstract class AuditableDataModel
{
    private protected AuditableDataModel(Auditable auditable)
    {
        CreatedBy = auditable.CreatedBy;
        CreatedOnUtc = auditable.CreatedOnUtc;

        LastUpdatedBy = auditable.LastUpdatedBy;
        LastUpdatedOnUtc = auditable.LastUpdatedOnUtc;

        DisabledBy = auditable.DisabledBy;
        DisabledOnUtc = auditable.DisabledOnUtc;

        ReenabledBy = auditable.ReenabledBy;
        ReenabledOnUtc = auditable.ReenabledOnUtc;
    }

    public FonbecWebUser CreatedBy { get; }
    public DateTime CreatedOnUtc { get; }

    public FonbecWebUser? LastUpdatedBy { get; }
    public DateTime? LastUpdatedOnUtc { get; }

    public FonbecWebUser? DisabledBy { get; }
    public DateTime? DisabledOnUtc { get; }

    public FonbecWebUser? ReenabledBy { get; }
    public DateTime? ReenabledOnUtc { get; }
}