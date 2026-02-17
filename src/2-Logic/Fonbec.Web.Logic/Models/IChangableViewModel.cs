using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Services;
using Mapster;

namespace Fonbec.Web.Logic.Models;

public interface IChangableViewModel<TViewModel>
{
    TViewModel DeepClone();
    bool Equals(TViewModel other);
}

public abstract class ChangableViewModel<TViewModel> : IChangableViewModel<TViewModel>
{
    public TViewModel DeepClone()
    {
        return this.Adapt<TViewModel>();
    }

    public bool Equals(TViewModel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return IsIdenticalTo(other);
    }

    public abstract bool IsIdenticalTo(TViewModel other);
}