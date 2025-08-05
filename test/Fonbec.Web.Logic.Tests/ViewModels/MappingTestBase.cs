using Mapster;

namespace Fonbec.Web.Logic.Tests.ViewModels;

public abstract class MappingTestBase
{
    protected TypeAdapterConfig Config;

    protected MappingTestBase()
    {
        Config = TypeAdapterConfig.GlobalSettings;
        var assembly = System.Reflection.Assembly.Load("Fonbec.Web.Logic");
        
        Config.Scan(assembly);
    }
}