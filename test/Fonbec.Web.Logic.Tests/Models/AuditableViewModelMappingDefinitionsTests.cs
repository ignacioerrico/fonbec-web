using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels;
using Fonbec.Web.DataAccess.Entities;
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
        // Arrange
        Config.AllowImplicitDestinationInheritance = true;

        Auditable.LastUpdatedBy = new FonbecWebUser { FirstName = "FirstName UpdatedBy", LastName = "LastName UpdatedBy" };
        Auditable.LastUpdatedOnUtc = new DateTime(1996, 3, 15);
        Auditable.DisabledBy = new FonbecWebUser { FirstName = "FirstName DisabledBy", LastName = "LastName DisabledBy" };
        Auditable.DisabledOnUtc = new DateTime(1996, 3, 16);
        Auditable.ReenabledBy = new FonbecWebUser { FirstName = "FirstName ReenabledBy", LastName = "LastName ReenabledBy" };
        Auditable.ReenabledOnUtc = new DateTime(1996, 3, 17);
        Auditable.Notes = "Some personal notes";

        var dataModel = Substitute.For<DummyAuditableDataModel>(Auditable);

        // Act
        var viewModel = dataModel.Adapt<DummyAuditableViewModel>(Config);

        // Assert
        viewModel.CreatedBy.Should().Be("FirstName LastName");
        viewModel.CreatedOnUtc.Should().Be(new DateTime(1996, 3, 14));
        viewModel.LastUpdatedBy.Should().Be("FirstName UpdatedBy LastName UpdatedBy");
        viewModel.LastUpdatedOnUtc.Should().Be(new DateTime(1996, 3, 15));
        viewModel.DisabledBy.Should().Be("FirstName DisabledBy LastName DisabledBy");
        viewModel.DisabledOnUtc.Should().Be(new DateTime(1996, 3, 16));
        viewModel.ReenabledBy.Should().Be("FirstName ReenabledBy LastName ReenabledBy");
        viewModel.ReenabledOnUtc.Should().Be(new DateTime(1996, 3, 17));
        viewModel.Notes.Should().Be("Some personal notes");
    }

    [Fact]
    public void Maps_Auditable_Fields_Not_Set_To_Null()
    {
        // Arrange
        Config.AllowImplicitDestinationInheritance = true;

        var dataModel = Substitute.For<DummyAuditableDataModel>(Auditable);

        // Act
        var viewModel = dataModel.Adapt<DummyAuditableViewModel>(Config);

        // Assert
        viewModel.LastUpdatedBy.Should().BeNull();
        viewModel.LastUpdatedOnUtc.Should().BeNull();
        viewModel.DisabledBy.Should().BeNull();
        viewModel.DisabledOnUtc.Should().BeNull();
        viewModel.ReenabledBy.Should().BeNull();
        viewModel.ReenabledOnUtc.Should().BeNull();
    }

    [Fact]
    public void Maps_InputModel_NullNotes_To_DataModel_Empty()
    {
        // Arrange
        Config.AllowImplicitDestinationInheritance = true;

        Auditable.Notes = null;

        var dataModel = Substitute.For<DummyAuditableDataModel>(Auditable);

        // Act
        var viewModel = dataModel.Adapt<DummyAuditableViewModel>(Config);

        // Assert
        viewModel.Notes.Should().BeEmpty();
    }
}