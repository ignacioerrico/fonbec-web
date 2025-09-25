namespace Fonbec.Web.DataAccess.Entities.Abstract;

public abstract class Auditable
{
    private int? _disabledById;

    public int CreatedById { get; set; }
    public FonbecWebUser CreatedBy { get; set; } = null!;
    public DateTime CreatedOnUtc { get; set; }

    public int? LastUpdatedById { get; set; }
    public FonbecWebUser? LastUpdatedBy { get; set; } = null!;
    public DateTime? LastUpdatedOnUtc { get; set; }

    /// <summary>
    /// Setting this property to a non-null value will set <see cref="ReenabledById"/> and <see cref="ReenabledOnUtc"/> to null.
    /// Once set, this property will not be set to null again.
    /// </summary>
    public int? DisabledById
    {
        get => _disabledById;
        set
        {
            if (value is null)
            {
                return;
            }

            ReenabledById = null;
            ReenabledOnUtc = null;
            _disabledById = value;
        }
    }

    public FonbecWebUser? DisabledBy { get; set; } = null!;
    public DateTime? DisabledOnUtc { get; set; }

    public int? ReenabledById { get; set; }
    public FonbecWebUser? ReenabledBy { get; set; } = null!;
    public DateTime? ReenabledOnUtc { get; set; }

    public bool IsActive { get; set; }
}