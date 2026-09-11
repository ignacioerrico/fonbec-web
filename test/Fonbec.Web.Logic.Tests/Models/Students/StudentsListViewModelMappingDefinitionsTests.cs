using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Students;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Sponsorships;
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
            StudentNickName = "JS",
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
            StudentPhoneNumber = "555-1234",
            StudentChapterName = "Cordoba",
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
        viewModel.StudentChapterName.Should().Be("Cordoba");
    }

    [Fact]
    public void Maps_Nullable_Fields_To_Empty_Or_Default()
    {
        var dataModel = new AllStudentsDataModel(Auditable)
        {
            StudentNickName = null,
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

    [Fact]
    public void Maps_Sponsor_Periods_And_Ignores_NonCurrent_When_Checking_HasActiveSponsors()
    {
        var finishedStart = DateTime.UtcNow.AddYears(-2);
        var finishedEnd = DateTime.UtcNow.AddMonths(-2);
        var activeStart = DateTime.UtcNow.AddMonths(-1);
        var activeEnd = DateTime.UtcNow.AddMonths(1);
        var upcomingStart = DateTime.UtcNow.AddMonths(2);
        var upcomingEnd = DateTime.UtcNow.AddMonths(8);

        var dataModel = new AllStudentsDataModel(Auditable)
        {
            StudentFirstName = "Ana",
            StudentLastName = "Becaria",
            ActiveSponsors =
            [
                new StudentActiveSponsorDataModel
                {
                    Name = "Padrino Pasado",
                    StartDate = finishedStart,
                    EndDate = finishedEnd,
                },
                new StudentActiveSponsorDataModel
                {
                    Name = "Padrino Actual",
                    IsCompany = true,
                    StartDate = activeStart,
                    EndDate = activeEnd,
                },
                new StudentActiveSponsorDataModel
                {
                    Name = "Padrino Futuro",
                    StartDate = upcomingStart,
                    EndDate = upcomingEnd,
                },
            ],
        };

        var viewModel = dataModel.Adapt<StudentsListViewModel>(Config);

        viewModel.ActiveSponsors.Should().HaveCount(3);
        viewModel.HasActiveSponsors.Should().BeTrue();

        viewModel.ActiveSponsors[0].Name.Should().Be("Padrino Pasado");
        viewModel.ActiveSponsors[0].StartDate.Should().Be(finishedStart);
        viewModel.ActiveSponsors[0].EndDate.Should().Be(finishedEnd);
        viewModel.ActiveSponsors[0].TimelineStatus.Should().Be(SponsorshipTimelineStatus.Finished);
        viewModel.ActiveSponsors[0].IsCurrentlyActive.Should().BeFalse();
        viewModel.ActiveSponsors[0].PeriodTooltipLine.Should().Be($"Padrino Pasado · {finishedEnd.ToSpanishMonthYear()}");

        viewModel.ActiveSponsors[1].Name.Should().Be("Padrino Actual");
        viewModel.ActiveSponsors[1].IsCompany.Should().BeTrue();
        viewModel.ActiveSponsors[1].TimelineStatus.Should().Be(SponsorshipTimelineStatus.Active);
        viewModel.ActiveSponsors[1].IsCurrentlyActive.Should().BeTrue();

        viewModel.ActiveSponsors[2].Name.Should().Be("Padrino Futuro");
        viewModel.ActiveSponsors[2].TimelineStatus.Should().Be(SponsorshipTimelineStatus.NotStarted);
        viewModel.ActiveSponsors[2].IsCurrentlyActive.Should().BeFalse();
        viewModel.ActiveSponsors[2].PeriodTooltipLine.Should().Be($"Padrino Futuro · {upcomingStart.ToSpanishMonthYear()}");
    }

    [Fact]
    public void PeriodTooltip_Shows_Both_Months_When_The_Period_Has_An_End()
    {
        var sponsor = new StudentActiveSponsorViewModel
        {
            Name = "Elena Actual",
            StartDate = new DateTime(2025, 3, 1),
            EndDate = new DateTime(2026, 6, 30),
        };

        sponsor.PeriodTooltip.Should().Be("Marzo de 2025 – Junio de 2026");
    }

    [Fact]
    public void PeriodTooltip_Shows_Only_The_Start_When_The_Period_Is_OpenEnded()
    {
        var sponsor = new StudentActiveSponsorViewModel
        {
            Name = "Elena Actual",
            StartDate = new DateTime(2025, 3, 1),
            EndDate = null,
        };

        sponsor.PeriodTooltip.Should().Be("Desde Marzo de 2025");
    }

    [Fact]
    public void HasActiveSponsors_Is_False_When_Only_Finished_Or_Upcoming()
    {
        var dataModel = new AllStudentsDataModel(Auditable)
        {
            ActiveSponsors =
            [
                new StudentActiveSponsorDataModel
                {
                    Name = "Pasado",
                    StartDate = DateTime.UtcNow.AddYears(-1),
                    EndDate = DateTime.UtcNow.AddMonths(-1),
                },
                new StudentActiveSponsorDataModel
                {
                    Name = "Futuro",
                    StartDate = DateTime.UtcNow.AddMonths(1),
                    EndDate = DateTime.UtcNow.AddMonths(6),
                },
            ],
        };

        var viewModel = dataModel.Adapt<StudentsListViewModel>(Config);

        viewModel.HasActiveSponsors.Should().BeFalse();
    }
}