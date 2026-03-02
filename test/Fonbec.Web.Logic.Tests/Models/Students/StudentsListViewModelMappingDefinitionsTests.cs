using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Students;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Students;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Students;

public class StudentsListViewModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_All_Fields_Correctly_From_AllStudentsDataModel()
    {
        var now = DateTime.UtcNow;
        var dataModel = new AllStudentsDataModel(Auditable)
        {
            StudentId = 10,
            StudentFirstName = "Jane",
            StudentLastName = "Smith",
            StundentNickName = "JS",
            StudentGender = Gender.Female,
            IsStudentActive = true,
            FacilitatorId = 5,
            FacilitatorFirstName = "Fac",
            FacilitatorLastName = "Tilitator",
            FacilitatorEmail = "facilitator@email.com",
            StudentEmail = "student@email.com",
            Notes = "Some notes",
            StudentCurrentEducationLevel = EducationLevel.SecondarySchool,
            StudentSecondarySchoolStartYear = now,
            StudentUniversityStartYear = now.AddYears(2),
            StudentPhoneNumber = "555-1234"
        };

        var viewModel = dataModel.Adapt<StudentsListViewModel>(Config);

        viewModel.StudentId.Should().Be(10);
        viewModel.StudentFirstName.Should().Be("Jane");
        viewModel.StudentLastName.Should().Be("Smith");
        viewModel.StudentNickName.Should().Be("JS");
        viewModel.StudentGender.Should().Be(Gender.Female);
        viewModel.IsStudentActive.Should().BeTrue();
        viewModel.FacilitatorId.Should().Be(5);
        viewModel.FacilitatorFullName.Should().Be("Fac Tilitator");
        viewModel.FacilitatorEmail.Should().Be("facilitator@email.com");
        viewModel.StudentEmail.Should().Be("student@email.com");
        viewModel.Notes.Should().Be("Some notes");
        viewModel.StudentCurrentEducationLevel.Should().Be("Secundario");
        viewModel.StudentSecondarySchoolStartYear.Should().Be(now);
        viewModel.StudentUniversityStartYear.Should().Be(now.AddYears(2));
        viewModel.StudentPhoneNumber.Should().Be("555-1234");
    }

    [Fact]
    public void Maps_Nullable_Fields_To_Empty_Or_Default()
    {
        var dataModel = new AllStudentsDataModel(Auditable)
        {
            StundentNickName = null,
            FacilitatorEmail = null,
            StudentEmail = null,
            Notes = null,
            StudentPhoneNumber = null
        };

        var viewModel = dataModel.Adapt<StudentsListViewModel>(Config);

        viewModel.StudentNickName.Should().BeEmpty();
        viewModel.FacilitatorEmail.Should().BeEmpty();
        viewModel.StudentEmail.Should().BeEmpty();
        viewModel.Notes.Should().BeEmpty();
        viewModel.StudentPhoneNumber.Should().BeEmpty();
    }

    [Fact]
    public void IsIdenticalTo_Compares_FirstName_LastName_NickName_Gender_FacilitatorId_EducationLevel_Email_PhoneNumber_Notes()
    {
        var studentsListViewModel1 = new StudentsListViewModel
        {
            StudentId = 314,
            StudentFirstName = "First Name",
            StudentLastName = "Last Name",
            StudentNickName = "Nick Name",
            StudentGender = Gender.Male,
            IsStudentActive = true,
            FacilitatorId = 512,
            FacilitatorFullName = "FacilitatorFullName1",
            FacilitatorEmail = "FacilitatorEmail1@mail.com",
            StudentEmail = "student-email@mail.com",
            Notes = "Notes",
            StudentCurrentEducationLevel = "EducationLevel",
            StudentPhoneNumber = "PhoneNumber",
        };

        var studentsListViewModel2 = new StudentsListViewModel
        {
            StudentId = 315,
            StudentFirstName = "First Name",
            StudentLastName = "Last Name",
            StudentNickName = "Nick Name",
            StudentGender = Gender.Male,
            IsStudentActive = false,
            FacilitatorId = 512,
            FacilitatorFullName = "FacilitatorFullName2",
            FacilitatorEmail = "FacilitatorEmail2@mail.com",
            StudentEmail = "student-email@mail.com",
            Notes = "Notes",
            StudentCurrentEducationLevel = "EducationLevel",
            StudentPhoneNumber = "PhoneNumber",
        };

        var result = studentsListViewModel1.IsIdenticalTo(studentsListViewModel2);

        result.Should().BeTrue();
    }
}