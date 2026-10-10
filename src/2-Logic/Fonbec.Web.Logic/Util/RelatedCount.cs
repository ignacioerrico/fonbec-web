namespace Fonbec.Web.Logic.Util;

/// <summary>
/// How many related people to keep. Values are ordered for a four-stop slider:
/// none, exactly one, two or more, then any amount.
/// </summary>
public enum RelatedCount
{
    None = 0,
    One = 1,
    TwoOrMore = 2,
    Any = 3,
}

public static class RelatedCountExtensions
{
    public static bool Matches(this RelatedCount filter, int count) => filter switch
    {
        RelatedCount.None => count == 0,
        RelatedCount.One => count == 1,
        RelatedCount.TwoOrMore => count >= 2,
        RelatedCount.Any => true,
        _ => false,
    };
}