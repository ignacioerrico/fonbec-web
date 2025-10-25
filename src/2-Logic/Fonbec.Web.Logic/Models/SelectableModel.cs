namespace Fonbec.Web.Logic.Models;

public class SelectableModel<TKey>(TKey key, string displayName, string displayDescription) : IEquatable<SelectableModel<TKey>>
    where TKey : struct
{
    public TKey Key { get; init; } = key;

    public string DisplayName { get; set; } = displayName;

    public string DisplayDescription { get; set; } = displayDescription;

    public override string ToString() => DisplayName;

    #region Define equality - Required by MudSelect

    public static bool operator ==(SelectableModel<TKey>? left, SelectableModel<TKey>? right) => Equals(left, right);

    public static bool operator !=(SelectableModel<TKey>? left, SelectableModel<TKey>? right) => !(left == right);

    /// <summary>
    /// System.Object Equals override: to avoid reflection, which is slow
    /// </summary>
    /// <param name="obj">Object being compared to</param>
    /// <returns>Whether the object equals the current instance</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((SelectableModel<TKey>)obj);
    }

    /// <summary>
    /// IEquatable implementation
    /// </summary>
    /// <param name="other">Object being compared to</param>
    /// <returns>Whether the object equals the current instance, based on the Key</returns>
    public bool Equals(SelectableModel<TKey>? other)
    {
        if (other is null) return false;
        return ReferenceEquals(this, other) || Key.Equals(other.Key);
    }

    /// <summary>
    /// Required for equality comparison
    /// </summary>
    /// <returns>Hash code based on the Key</returns>
    public override int GetHashCode() => Key.GetHashCode();

    #endregion
}