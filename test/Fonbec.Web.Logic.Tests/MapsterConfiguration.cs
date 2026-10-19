using System.Runtime.CompilerServices;
using Fonbec.Web.Logic.Services;
using Mapster;

namespace Fonbec.Web.Logic.Tests;

internal static class MapsterConfiguration
{
    [ModuleInitializer]
    internal static void Register()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(FacilitatorService).Assembly);
    }
}
