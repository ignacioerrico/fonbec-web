using Mapster;

namespace Fonbec.Web.Logic.ExtensionMethods;

public interface IDetectChanges<T>
{
    /// <summary>
    /// This is normally used to detect changes when updating a view model.
    /// </summary>
    /// <param name="other">The class (typically a view model) to compare to.</param>
    /// <returns>True if the properties that are edited by the user when updating a view model have not changed; false otherwise.</returns>
    bool IsIdenticalTo(T other);
}

public static class DetectChangesExtensionMethods
{
    extension<T>(T @this) where T : IDetectChanges<T>
    {
        public T DeepClone()
        {
            return @this.Adapt<T>();
        }

        public bool IsEqualTo(T? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(@this, other)) return true;

            return @this.IsIdenticalTo(other);
        }
    }
}