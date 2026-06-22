using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Students;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Students;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Students;
public class FacilitatorStudentsListViewModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_All_Fields_Correctly_From_FacilitatorStudentsDataModel()
    {
        var now = DateTime.UtcNow;
        var dataModel = new FacilitatorStudentsDataModel(Auditable)
        {
            StudentId = 10,
            StudentFirstName = "Jhon",
            StudentLastName = "Cena",
            Notes = "Some notes",
        };

        var viewModel = dataModel.Adapt<FacilitatorStudentsListViewModel>(Config);

        viewModel.StudentId.Should().Be(10);
        viewModel.StudentFirstName.Should().Be("Jhon");
        viewModel.StudentLastName.Should().Be("Cena");
        viewModel.Notes.Should().Be("Some notes");
    }

    [Fact]
    public void Maps_Nullable_Field_To_Empty_Or_Default()
    {
        var dataModel = new FacilitatorStudentsDataModel(Auditable)
        {
            Notes = null
        };

        var viewModel = dataModel.Adapt<FacilitatorStudentsListViewModel>(Config);

        viewModel.Notes.Should().BeEmpty();
    }

    [Fact]
    public void IsIdenticalTo_Compares_FirstName_LastName_Notes()
    {
        var studentsListViewModel1 = new FacilitatorStudentsListViewModel
        {
            StudentId = 121,
            StudentFirstName = "First Name",
            StudentLastName = "Last Name",
            Notes = "Notes"
        };

        var studentsListViewModel2 = new FacilitatorStudentsListViewModel
        {
            StudentId = 121,
            StudentFirstName = "First Name",
            StudentLastName = "Last Name",
            Notes = "Notes"
        };

        var result = studentsListViewModel1.IsIdenticalTo(studentsListViewModel2);

        result.Should().BeTrue();
    }
}
