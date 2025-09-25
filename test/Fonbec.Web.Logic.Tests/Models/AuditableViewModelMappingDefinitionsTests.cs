using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels;
using Fonbec.Web.DataAccess.Entities.Abstract;
using Fonbec.Web.Logic.Models;
using Mapster;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Models;

public class AuditableViewModelMappingDefinitionsTests : MappingTestBase
{
    public class DummyAuditableDataModel(Auditable auditable) : AuditableDataModel(auditable);

    public class DummyAuditableViewModel : AuditableViewModel;

    [Fact]
    public void Maps_All_Fields_Correctly_From_AuditableDataModel()
    {
        Config.AllowImplicitDestinationInheritance = true;

        var dataModel = Substitute.For<DummyAuditableDataModel>(Auditable);

        var viewModel = dataModel.Adapt<DummyAuditableViewModel>(Config);

        viewModel.CreatedBy.Should().Be("FirstName LastName");
        viewModel.CreatedOnUtc.Should().Be(Auditable.CreatedOnUtc);
        
        // The rest are null by default in MappingTestBase's Auditable mock
        viewModel.LastUpdatedBy.Should().BeNull();
        viewModel.LastUpdatedOnUtc.Should().BeNull();
        viewModel.DisabledBy.Should().BeNull();
        viewModel.DisabledOnUtc.Should().BeNull();
        viewModel.ReenabledBy.Should().BeNull();
        viewModel.ReenabledOnUtc.Should().BeNull();
    }
}