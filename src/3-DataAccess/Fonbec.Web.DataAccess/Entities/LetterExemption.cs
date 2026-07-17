namespace Fonbec.Web.DataAccess.Entities;

public class LetterExemption
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int PlannedDeliveryId { get; set; }
    public PlannedDelivery PlannedDelivery { get; set; } = null!;

    /// <summary>Denormalized from the student for chapter-scoped queries.</summary>
    public int ChapterId { get; set; }

    public string Reason { get; set; } = null!;

    public int CreatedByFonbecUserId { get; set; }
    public DateTime CreatedOnUtc { get; set; }

    public bool IsRevoked { get; set; }
    public int? RevokedByFonbecUserId { get; set; }
    public DateTime? RevokedOnUtc { get; set; }
}