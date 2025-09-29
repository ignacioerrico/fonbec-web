using Fonbec.Web.Ui.Components;
using Fonbec.Web.Ui.Components.Pages;
using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace Fonbec.Web.Ui.Authorization;

public static class PageAccessDiscovery
{
    public static List<PageAccessInfo> DiscoverPages()
    {
        // Find all components in the assembly that have the PageMetadataAttribute
        var pageMetadataAttributes = typeof(App).Assembly.ExportedTypes
            .Where(type => typeof(IComponent).IsAssignableFrom(type))
            .Select(type => type.GetCustomAttribute<PageMetadataAttribute>(false))
            .Where(pageMetadata => pageMetadata is not null);

        var results = pageMetadataAttributes.Select(pageMetadata =>
            new PageAccessInfo(pageMetadata!.Codename, pageMetadata.Description))
            .OrderBy(pai => pai.Codename)
            .ToList();

        return results;
    }
}