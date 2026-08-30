namespace Fonbec.Web.DataAccess.Entities;

/// <summary>
/// Singleton cursor used to round-robin review dequeue across chapters.
/// </summary>
public class ReviewQueueCursor
{
    public const int SingletonId = 1;

    public int Id { get; set; }

    /// <summary>Chapter served by the last successful take-next; <c>null</c> before the first take.</summary>
    public int? LastServedChapterId { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}