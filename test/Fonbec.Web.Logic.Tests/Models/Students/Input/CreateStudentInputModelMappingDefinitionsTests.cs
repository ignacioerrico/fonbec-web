using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Students.Input;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Students.Input;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Students.Input;

public class CreateStudentInputModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_All_Fields_Correctly_When_All_Values_Present()
    {
        var input = new CreateStudentInputModel(
            ChapterId: 1,
            StudentFirstName: "Jane",
            StudentLastName: "Smith",
            StudentNickName: "JS",
            StudentGender: Gender.Female,
            StudentEmail: "jane@x.com",
            StudentPhone: "555-1234",
            StudentNotes: "Some notes",
            StudentSecondarySchoolStartYear: new DateTime(2020, 1, 1),
            StudentUniversityStartYear: new DateTime(2022, 1, 1),
            FacilitatorId: 2,
            CreatedById: 99
        );

        var result = input.Adapt<CreateStudentInputDataModel>(Config);

        result.ChapterId.Should().Be(1);
        result.StudentFirstName.Should().Be("Jane");
        result.StudentLastName.Should().Be("Smith");
        result.StudentNickName.Should().Be("Js");
        result.StudentGender.Should().Be(Gender.Female);
        result.StudentEmail.Should().Be("jane@x.com");
        result.StudentPhoneNumber.Should().Be("555-1234");
        result.StudentNotes.Should().Be("Some notes");
        result.StudentSecondarySchoolStartYear.Should().Be(new DateTime(2020, 1, 1));
        result.StudentUniversityStartYear.Should().Be(new DateTime(2022, 1, 1));
        result.FacilitatorId.Should().Be(2);
        result.CreatedById.Should().Be(99);
    }

    [Fact]
    public void InputModel_StudentFirstName_MustBeNonEmpty()
    {
        var input = new CreateStudentInputModel(
            ChapterId: 1,
            StudentFirstName: string.Empty,
            StudentLastName: "Smith",
            StudentNickName: "JS",
            StudentGender: Gender.Female,
            StudentEmail: "",
            StudentPhone: "555-1234",
            StudentNotes: "Some notes",
            StudentSecondarySchoolStartYear: null,
            StudentUniversityStartYear: null,
            FacilitatorId: 2,
            CreatedById: 99
        );

        var result = () => input.Adapt<CreateStudentInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Fact]
    public void InputModel_StudentFirstName_IsNormalized()
    {
        var input = new CreateStudentInputModel(
            ChapterId: 1,
            StudentFirstName: "  studEnt fiRSt naMe   ",
            StudentLastName: "Smith",
            StudentNickName: "JS",
            StudentGender: Gender.Female,
            StudentEmail: "",
            StudentPhone: "555-1234",
            StudentNotes: "Some notes",
            StudentSecondarySchoolStartYear: null,
            StudentUniversityStartYear: null,
            FacilitatorId: 2,
            CreatedById: 99
        );

        var result = input.Adapt<CreateStudentInputDataModel>(Config);

        result.StudentFirstName.Should().Be("Student First Name");
    }

    [Fact]
    public void InputModel_StudentLastName_MustBeNonEmpty()
    {
        var input = new CreateStudentInputModel(
            ChapterId: 1,
            StudentFirstName: "Jane",
            StudentLastName: string.Empty,
            StudentNickName: "JS",
            StudentGender: Gender.Female,
            StudentEmail: "jane@x.com",
            StudentPhone: "555-1234",
            StudentNotes: "Some notes",
            StudentSecondarySchoolStartYear: null,
            StudentUniversityStartYear: null,
            FacilitatorId: 2,
            CreatedById: 99
        );

        var result = () => input.Adapt<CreateStudentInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Fact]
    public void InputModel_StudentLastName_IsNormalized()
    {
        var input = new CreateStudentInputModel(
            ChapterId: 1,
            StudentFirstName: "Jane",
            StudentLastName: "  studEnt laSt naMe   ",
            StudentNickName: "JS",
            StudentGender: Gender.Female,
            StudentEmail: "jane@x.com",
            StudentPhone: "555-1234",
            StudentNotes: "Some notes",
            StudentSecondarySchoolStartYear: null,
            StudentUniversityStartYear: null,
            FacilitatorId: 2,
            CreatedById: 99
        );

        var result = input.Adapt<CreateStudentInputDataModel>(Config);

        result.StudentLastName.Should().Be("Student Last Name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Does_Not_Map_StudentNickName_When_Empty_Or_Whitespace(string nickName)
    {
        var input = new CreateStudentInputModel(
            ChapterId: 1,
            StudentFirstName: "Jane",
            StudentLastName: "Smith",
            StudentNickName: nickName,
            StudentGender: Gender.Female,
            StudentEmail: "jane@x.com",
            StudentPhone: "555-1234",
            StudentNotes: "Some notes",
            StudentSecondarySchoolStartYear: null,
            StudentUniversityStartYear: null,
            FacilitatorId: 2,
            CreatedById: 99
        );

        var result = input.Adapt<CreateStudentInputDataModel>(Config);
        
        result.StudentNickName.Should().BeNull();
    }

    [Fact]
    public void InputModel_StudentNickName_IsNormalized()
    {
        var input = new CreateStudentInputModel(
            ChapterId: 1,
            StudentFirstName: "Jane",
            StudentLastName: "Smith",
            StudentNickName: "  studEnt  niCKnaMe   ",
            StudentGender: Gender.Female,
            StudentEmail: "jane@x.com",
            StudentPhone: "555-1234",
            StudentNotes: "Some notes",
            StudentSecondarySchoolStartYear: null,
            StudentUniversityStartYear: null,
            FacilitatorId: 2,
            CreatedById: 99
        );

        var result = input.Adapt<CreateStudentInputDataModel>(Config);

        result.StudentNickName.Should().Be("Student Nickname");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Does_Not_Map_StudentEmail_When_Empty_Or_Whitespace(string email)
    {
        var input = new CreateStudentInputModel(
            ChapterId: 1,
            StudentFirstName: "Jane",
            StudentLastName: "Smith",
            StudentNickName: "JS",
            StudentGender: Gender.Female,
            StudentEmail: email,
            StudentPhone: "555-1234",
            StudentNotes: "Some notes",
            StudentSecondarySchoolStartYear: null,
            StudentUniversityStartYear: null,
            FacilitatorId: 2,
            CreatedById: 99
        );

        var result = input.Adapt<CreateStudentInputDataModel>(Config);

        result.StudentEmail.Should().BeNull();
    }

    [Fact]
    public void InputModel_StudentEmail_IsTrimmedAndLowered()
    {
        var input = new CreateStudentInputModel(
            ChapterId: 1,
            StudentFirstName: "Jane",
            StudentLastName: "Smith",
            StudentNickName: "JS",
            StudentGender: Gender.Female,
            StudentEmail: "  jaNE@x.Com   ",
            StudentPhone: "555-1234",
            StudentNotes: "Some notes",
            StudentSecondarySchoolStartYear: null,
            StudentUniversityStartYear: null,
            FacilitatorId: 2,
            CreatedById: 99
        );

        var result = input.Adapt<CreateStudentInputDataModel>(Config);

        result.StudentEmail.Should().Be("jane@x.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Does_Not_Map_StudentPhone_When_Empty_Or_Whitespace(string phone)
    {
        var input = new CreateStudentInputModel(
            ChapterId: 1,
            StudentFirstName: "Jane",
            StudentLastName: "Smith",
            StudentNickName: "JS",
            StudentGender: Gender.Female,
            StudentEmail: "jane@x.com",
            StudentPhone: phone,
            StudentNotes: "Some notes",
            StudentSecondarySchoolStartYear: null,
            StudentUniversityStartYear: null,
            FacilitatorId: 2,
            CreatedById: 99
        );

        var result = input.Adapt<CreateStudentInputDataModel>(Config);

        result.StudentPhoneNumber.Should().BeNull();
    }

    [Fact]
    public void InputModel_StudentPhone_IsTrimmed()
    {
        var input = new CreateStudentInputModel(
            ChapterId: 1,
            StudentFirstName: "Jane",
            StudentLastName: "Smith",
            StudentNickName: "JS",
            StudentGender: Gender.Female,
            StudentEmail: "jane@x.com",
            StudentPhone: "  555-1234   ",
            StudentNotes: "Some notes",
            StudentSecondarySchoolStartYear: null,
            StudentUniversityStartYear: null,
            FacilitatorId: 2,
            CreatedById: 99
        );

        var result = input.Adapt<CreateStudentInputDataModel>(Config);

        result.StudentPhoneNumber.Should().Be("555-1234");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Does_Not_Map_StudentNotes_When_Empty_Or_Whitespace(string notes)
    {
        var input = new CreateStudentInputModel(
            ChapterId: 1,
            StudentFirstName: "Jane",
            StudentLastName: "Smith",
            StudentNickName: "JS",
            StudentGender: Gender.Female,
            StudentEmail: "jane@x.com",
            StudentPhone: "555-1234",
            StudentNotes: notes,
            StudentSecondarySchoolStartYear: null,
            StudentUniversityStartYear: null,
            FacilitatorId: 2,
            CreatedById: 99
        );

        var result = input.Adapt<CreateStudentInputDataModel>(Config);

        result.StudentNotes.Should().BeNull();
    }

    [Fact]
    public void InputModel_StudentNotes_IsTrimmed()
    {
        var input = new CreateStudentInputModel(
            ChapterId: 1,
            StudentFirstName: "Jane",
            StudentLastName: "Smith",
            StudentNickName: "JS",
            StudentGender: Gender.Female,
            StudentEmail: "jane@x.com",
            StudentPhone: "555-1234",
            StudentNotes: "  Some notes   ",
            StudentSecondarySchoolStartYear: null,
            StudentUniversityStartYear: null,
            FacilitatorId: 2,
            CreatedById: 99
        );

        var result = input.Adapt<CreateStudentInputDataModel>(Config);

        result.StudentNotes.Should().Be("Some notes");
    }
}