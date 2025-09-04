using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Abstract;
using Mapster;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.ViewModels;

public abstract class MappingTestBase
{
    protected TypeAdapterConfig Config;

    protected readonly Auditable Auditable;

    protected MappingTestBase()
    {
        // Mapster configuration
        Config = TypeAdapterConfig.GlobalSettings;
        var assembly = System.Reflection.Assembly.Load("Fonbec.Web.Logic");
        
        Config.Scan(assembly);

        // Auditable mock
        Auditable = Substitute.For<Auditable>();
        Auditable.CreatedBy = new FonbecWebUser { FirstName = "FirstName", LastName = "LastName" };
    }
}