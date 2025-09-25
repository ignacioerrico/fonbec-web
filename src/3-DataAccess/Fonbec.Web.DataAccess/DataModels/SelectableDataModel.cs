namespace Fonbec.Web.DataAccess.DataModels;

public record SelectableDataModel<TKey>(
    TKey Key,
    string Value
) where TKey : struct;