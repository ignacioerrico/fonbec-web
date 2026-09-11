using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class SponsorshipRepositoryTests
{
    private const int StudentId = 10;
    private const int SponsorId = 20;
    private const int OtherSponsorId = 21;
    private const int CompanyId = 30;
    private const int UserId = 40;

    [Fact]
    public async Task CreateSponsorshipAsync_Rejects_Overlapping_Period()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 6));
        var repository = new SponsorshipRepository(factory);
        var input = SponsorInput(new DateTime(2026, 5, 15), endDate: null);

        var preview = await repository.GetSponsorshipPeriodMatchAsync(input);
        var result = await repository.CreateSponsorshipAsync(input);

        preview.Should().Be(SponsorshipPeriodMatch.Overlap);
        result.PeriodMatch.Should().Be(SponsorshipPeriodMatch.Overlap);
        result.AffectedRows.Should().Be(0);
        (await GetActiveSponsorshipsAsync(factory)).Should().ContainSingle();
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Rejects_Period_Overlapping_OpenEnded_Sponsorship()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(factory, January(2026), endDate: null);
        var repository = new SponsorshipRepository(factory);

        var result = await repository.CreateSponsorshipAsync(
            SponsorInput(new DateTime(2027, 1, 1), EndOfMonth(2027, 3)));

        result.PeriodMatch.Should().Be(SponsorshipPeriodMatch.Overlap);
        result.AffectedRows.Should().Be(0);
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Extends_Adjacent_Period_Forward()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 6));
        var repository = new SponsorshipRepository(factory);
        var input = SponsorInput(new DateTime(2026, 7, 12), new DateTime(2026, 9, 3));

        var preview = await repository.GetSponsorshipPeriodMatchAsync(input);
        var result = await repository.CreateSponsorshipAsync(input);

        preview.Should().Be(SponsorshipPeriodMatch.Adjacent);
        result.PeriodMatch.Should().Be(SponsorshipPeriodMatch.Adjacent);
        result.AffectedRows.Should().BeGreaterThan(0);
        var sponsorship = (await GetActiveSponsorshipsAsync(factory)).Should().ContainSingle().Which;
        sponsorship.StartDate.Should().Be(January(2026));
        sponsorship.EndDate.Should().Be(EndOfMonth(2026, 9));
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Extends_Adjacent_Period_Backward()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(factory, new DateTime(2026, 7, 1), EndOfMonth(2026, 9));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.CreateSponsorshipAsync(
            SponsorInput(January(2026), EndOfMonth(2026, 6)));

        result.PeriodMatch.Should().Be(SponsorshipPeriodMatch.Adjacent);
        var sponsorship = (await GetActiveSponsorshipsAsync(factory)).Should().ContainSingle().Which;
        sponsorship.StartDate.Should().Be(January(2026));
        sponsorship.EndDate.Should().Be(EndOfMonth(2026, 9));
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Merges_Two_Periods_When_New_Period_Bridges_Them()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 3));
        await SeedSponsorshipAsync(factory, new DateTime(2026, 7, 1), EndOfMonth(2026, 9));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.CreateSponsorshipAsync(
            SponsorInput(new DateTime(2026, 4, 1), EndOfMonth(2026, 6)));

        result.PeriodMatch.Should().Be(SponsorshipPeriodMatch.Adjacent);
        var sponsorship = (await GetActiveSponsorshipsAsync(factory)).Should().ContainSingle().Which;
        sponsorship.StartDate.Should().Be(January(2026));
        sponsorship.EndDate.Should().Be(EndOfMonth(2026, 9));
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Creates_Separate_NonContiguous_Period()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 6));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.CreateSponsorshipAsync(
            SponsorInput(new DateTime(2026, 8, 1), EndOfMonth(2026, 9)));

        result.PeriodMatch.Should().Be(SponsorshipPeriodMatch.None);
        result.AffectedRows.Should().BeGreaterThan(0);
        (await GetActiveSponsorshipsAsync(factory)).Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Allows_Same_Period_For_Different_Sponsor()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 6));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.CreateSponsorshipAsync(
            SponsorInput(January(2026), EndOfMonth(2026, 6), OtherSponsorId));

        result.PeriodMatch.Should().Be(SponsorshipPeriodMatch.None);
        (await GetActiveSponsorshipsAsync(factory)).Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCreateSponsorshipPreviewAsync_Offers_To_End_Overlapping_Other_Sponsor()
    {
        var factory = CreateDbContextFactory();
        await SeedStudentAsync(factory);
        await SeedSponsorshipAsync(factory, new DateTime(2026, 3, 1), endDate: null);
        var repository = new SponsorshipRepository(factory);
        var input = SponsorInput(new DateTime(2026, 9, 1), endDate: null, OtherSponsorId);

        var preview = await repository.GetCreateSponsorshipPreviewAsync(input);

        preview.PeriodMatch.Should().Be(SponsorshipPeriodMatch.None);
        var overlapping = preview.OverlappingToEnd.Should().ContainSingle().Which;
        overlapping.RecipientName.Should().Be("Carlos Padrino");
        overlapping.ProposedEndDate.Should().Be(EndOfMonth(2026, 8));
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Ends_Overlapping_Other_Sponsor_When_Requested()
    {
        var factory = CreateDbContextFactory();
        await SeedStudentAsync(factory);
        await SeedSponsorshipAsync(factory, new DateTime(2026, 3, 1), endDate: null);
        var repository = new SponsorshipRepository(factory);
        var input = SponsorInput(new DateTime(2026, 9, 1), endDate: null, OtherSponsorId);
        input.EndOverlappingSponsorships = true;

        var result = await repository.CreateSponsorshipAsync(input);

        result.AffectedRows.Should().BeGreaterThan(0);
        var sponsorships = await GetActiveSponsorshipsAsync(factory);
        sponsorships.Should().HaveCount(2);
        sponsorships.Single(s => s.SponsorId == SponsorId).EndDate.Should().Be(EndOfMonth(2026, 8));
        var created = sponsorships.Single(s => s.SponsorId == OtherSponsorId);
        created.StartDate.Should().Be(new DateTime(2026, 9, 1));
        created.EndDate.Should().BeNull();
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Leaves_Overlapping_Other_Sponsor_When_Not_Requested()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(factory, new DateTime(2026, 3, 1), endDate: null);
        var repository = new SponsorshipRepository(factory);

        var result = await repository.CreateSponsorshipAsync(
            SponsorInput(new DateTime(2026, 9, 1), endDate: null, OtherSponsorId));

        result.AffectedRows.Should().BeGreaterThan(0);
        var previous = (await GetActiveSponsorshipsAsync(factory)).Single(s => s.SponsorId == SponsorId);
        previous.EndDate.Should().BeNull();
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Does_Not_End_Overlapping_When_It_Would_Uncover_Locked_Plan()
    {
        var factory = CreateDbContextFactory();
        await SeedStudentAsync(factory);
        await SeedSponsorshipAsync(factory, new DateTime(2026, 3, 1), endDate: null);
        await SeedUnsharedLetterAsync(factory, new DateTime(2026, 9, 1));
        var repository = new SponsorshipRepository(factory);
        var input = SponsorInput(new DateTime(2026, 9, 1), endDate: null, OtherSponsorId);
        input.EndOverlappingSponsorships = true;

        var result = await repository.CreateSponsorshipAsync(input);

        result.AffectedRows.Should().Be(0);
        result.UncoveredPlanStartsOn.Should().Equal(new DateTime(2026, 9, 1));
        (await GetActiveSponsorshipsAsync(factory)).Should().ContainSingle()
            .Which.EndDate.Should().BeNull();
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Applies_Extension_Rule_To_Company()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(
            factory,
            January(2026),
            EndOfMonth(2026, 6),
            sponsorId: null,
            companyId: CompanyId);
        var repository = new SponsorshipRepository(factory);

        var result = await repository.CreateSponsorshipAsync(
            CompanyInput(new DateTime(2026, 7, 1), EndOfMonth(2026, 9)));

        result.PeriodMatch.Should().Be(SponsorshipPeriodMatch.Adjacent);
        var sponsorship = (await GetActiveSponsorshipsAsync(factory)).Should().ContainSingle().Which;
        sponsorship.EndDate.Should().Be(EndOfMonth(2026, 9));
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Preserves_Existing_Notes_When_Extension_Notes_Are_Blank()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(
            factory,
            January(2026),
            EndOfMonth(2026, 6),
            notes: "Nota original");
        var repository = new SponsorshipRepository(factory);
        var input = SponsorInput(new DateTime(2026, 7, 1), EndOfMonth(2026, 9));
        input.SponsorshipNotes = " ";

        await repository.CreateSponsorshipAsync(input);

        var sponsorship = (await GetActiveSponsorshipsAsync(factory)).Should().ContainSingle().Which;
        sponsorship.Notes.Should().Be("Nota original");
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Replaces_Existing_Notes_When_Extension_Notes_Are_Provided()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(
            factory,
            January(2026),
            EndOfMonth(2026, 6),
            notes: "Nota original");
        var repository = new SponsorshipRepository(factory);
        var input = SponsorInput(new DateTime(2026, 7, 1), EndOfMonth(2026, 9));
        input.SponsorshipNotes = "Nota nueva";

        await repository.CreateSponsorshipAsync(input);

        var sponsorship = (await GetActiveSponsorshipsAsync(factory)).Should().ContainSingle().Which;
        sponsorship.Notes.Should().Be("Nota nueva");
    }

    [Fact]
    public async Task UpdateSponsorshipAsync_Rejects_Uncovering_Shared_Plan_Month()
    {
        var factory = CreateDbContextFactory();
        var sponsorshipId = await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 12));
        await SeedSharedLetterAsync(factory, new DateTime(2026, 9, 1));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.UpdateSponsorshipAsync(
            UpdateInput(sponsorshipId, January(2026), EndOfMonth(2026, 8)));

        result.Outcome.Should().Be(UpdateSponsorshipOutcome.UncoversLockedPlan);
        result.UncoveredPlanStartsOn.Should().Equal(new DateTime(2026, 9, 1));
        var sponsorship = (await GetActiveSponsorshipsAsync(factory)).Should().ContainSingle().Which;
        sponsorship.EndDate.Should().Be(EndOfMonth(2026, 12));
    }

    [Fact]
    public async Task UpdateSponsorshipAsync_Allows_Shrink_That_Still_Covers_Shared_Month()
    {
        var factory = CreateDbContextFactory();
        var sponsorshipId = await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 12));
        await SeedSharedLetterAsync(factory, new DateTime(2026, 9, 1));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.UpdateSponsorshipAsync(
            UpdateInput(sponsorshipId, January(2026), EndOfMonth(2026, 9)));

        result.Outcome.Should().Be(UpdateSponsorshipOutcome.Saved);
        result.AffectedRows.Should().BeGreaterThan(0);
        var sponsorship = (await GetActiveSponsorshipsAsync(factory)).Should().ContainSingle().Which;
        sponsorship.EndDate.Should().Be(EndOfMonth(2026, 9));
    }

    [Fact]
    public async Task UpdateSponsorshipAsync_Allows_Extend_Past_Locked_Month()
    {
        var factory = CreateDbContextFactory();
        var sponsorshipId = await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 9));
        await SeedSharedLetterAsync(factory, new DateTime(2026, 9, 1));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.UpdateSponsorshipAsync(
            UpdateInput(sponsorshipId, January(2026), EndOfMonth(2026, 12)));

        result.Outcome.Should().Be(UpdateSponsorshipOutcome.Saved);
        (await GetActiveSponsorshipsAsync(factory)).Single().EndDate.Should().Be(EndOfMonth(2026, 12));
    }

    [Fact]
    public async Task UpdateSponsorshipAsync_Rejects_Uncovering_Uploaded_Unshared_Letter()
    {
        var factory = CreateDbContextFactory();
        var sponsorshipId = await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 12));
        await SeedUnsharedLetterAsync(factory, new DateTime(2026, 9, 1));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.UpdateSponsorshipAsync(
            UpdateInput(sponsorshipId, January(2026), EndOfMonth(2026, 8)));

        result.Outcome.Should().Be(UpdateSponsorshipOutcome.UncoversLockedPlan);
        result.UncoveredPlanStartsOn.Should().Equal(new DateTime(2026, 9, 1));
        (await GetActiveSponsorshipsAsync(factory)).Single().EndDate.Should().Be(EndOfMonth(2026, 12));
    }

    [Fact]
    public async Task UpdateSponsorshipAsync_Allows_Shrink_When_No_Letter_Was_Uploaded()
    {
        var factory = CreateDbContextFactory();
        var sponsorshipId = await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 12));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.UpdateSponsorshipAsync(
            UpdateInput(sponsorshipId, January(2026), EndOfMonth(2026, 8)));

        result.Outcome.Should().Be(UpdateSponsorshipOutcome.Saved);
        (await GetActiveSponsorshipsAsync(factory)).Single().EndDate.Should().Be(EndOfMonth(2026, 8));
    }

    [Fact]
    public async Task UpdateSponsorshipAsync_Rejects_Overlap_With_Another_Period()
    {
        var factory = CreateDbContextFactory();
        var firstId = await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 6));
        await SeedSponsorshipAsync(factory, new DateTime(2026, 8, 1), EndOfMonth(2026, 9));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.UpdateSponsorshipAsync(
            UpdateInput(firstId, January(2026), EndOfMonth(2026, 8)));

        result.Outcome.Should().Be(UpdateSponsorshipOutcome.Overlap);
        (await GetActiveSponsorshipsAsync(factory)).Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateSponsorshipAsync_Merges_When_Period_Becomes_Adjacent()
    {
        var factory = CreateDbContextFactory();
        var firstId = await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 6));
        await SeedSponsorshipAsync(factory, new DateTime(2026, 8, 1), EndOfMonth(2026, 9));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.UpdateSponsorshipAsync(
            UpdateInput(firstId, January(2026), EndOfMonth(2026, 7)));

        result.Outcome.Should().Be(UpdateSponsorshipOutcome.Saved);
        result.PeriodMatch.Should().Be(SponsorshipPeriodMatch.Adjacent);
        var sponsorship = (await GetActiveSponsorshipsAsync(factory)).Should().ContainSingle().Which;
        sponsorship.Id.Should().Be(firstId);
        sponsorship.StartDate.Should().Be(January(2026));
        sponsorship.EndDate.Should().Be(EndOfMonth(2026, 9));
    }

    [Fact]
    public async Task GetAllSponsorshipsAsync_Includes_Uniquely_Covered_Locked_Months()
    {
        var factory = CreateDbContextFactory();
        await SeedStudentAsync(factory);
        await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 12));
        await SeedSharedLetterAsync(factory, new DateTime(2026, 9, 1));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.GetAllSponsorshipsAsync(StudentId);

        result.Sponsorships.Should().ContainSingle()
            .Which.LockedPlanStartsOn.Should().Equal(new DateTime(2026, 9, 1));
    }

    [Fact]
    public async Task GetAllSponsorshipsAsync_Returns_Student_Name_When_Student_Has_No_Sponsorships()
    {
        var factory = CreateDbContextFactory();
        await SeedStudentAsync(factory);
        var repository = new SponsorshipRepository(factory);

        var result = await repository.GetAllSponsorshipsAsync(StudentId);

        result.StudentFullName.Should().Be("Ana Becaria");
        result.Sponsorships.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllSponsorshipsAsync_Returns_No_Student_Name_When_Student_Does_Not_Exist()
    {
        var factory = CreateDbContextFactory();
        await SeedStudentAsync(factory);
        var repository = new SponsorshipRepository(factory);

        var result = await repository.GetAllSponsorshipsAsync(studentId: 999);

        result.StudentFullName.Should().BeNull();
        result.Sponsorships.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateSponsorshipAsync_Does_Not_Lock_Person_Sponsorship_From_Company_Letter_FanOut()
    {
        var factory = CreateDbContextFactory();
        var personSponsorshipId = await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 12));
        await SeedSponsorshipAsync(
            factory,
            January(2026),
            EndOfMonth(2026, 12),
            sponsorId: null,
            companyId: CompanyId);
        await SeedSharedCompanyLetterAsync(factory, new DateTime(2026, 9, 1), fanOutSponsorId: SponsorId);
        var repository = new SponsorshipRepository(factory);

        var result = await repository.UpdateSponsorshipAsync(
            UpdateInput(personSponsorshipId, January(2026), EndOfMonth(2026, 8)));

        result.Outcome.Should().Be(UpdateSponsorshipOutcome.Saved);
    }

    [Fact]
    public async Task UpdateSponsorshipAsync_Requires_Confirmation_When_Exemption_Would_Have_No_Slots()
    {
        var factory = CreateDbContextFactory();
        await SeedStudentAsync(factory);
        var sponsorshipId = await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 12));
        var planId = await SeedExemptionAsync(factory, new DateTime(2026, 9, 1));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.UpdateSponsorshipAsync(
            UpdateInput(sponsorshipId, January(2026), EndOfMonth(2026, 8)));

        result.Outcome.Should().Be(UpdateSponsorshipOutcome.RequiresExemptionRevocation);
        result.ExemptPlanStartsOn.Should().Equal(new DateTime(2026, 9, 1));
        (await GetActiveSponsorshipsAsync(factory)).Single().EndDate.Should().Be(EndOfMonth(2026, 12));
        (await GetExemptionAsync(factory, planId)).IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateSponsorshipAsync_Revokes_Orphaned_Exemption_After_Confirmation()
    {
        var factory = CreateDbContextFactory();
        await SeedStudentAsync(factory);
        var sponsorshipId = await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 12));
        var planId = await SeedExemptionAsync(factory, new DateTime(2026, 9, 1));
        var repository = new SponsorshipRepository(factory);
        var input = UpdateInput(sponsorshipId, January(2026), EndOfMonth(2026, 8));
        input.ConfirmExemptionRevocation = true;

        var result = await repository.UpdateSponsorshipAsync(input);

        result.Outcome.Should().Be(UpdateSponsorshipOutcome.Saved);
        (await GetActiveSponsorshipsAsync(factory)).Single().EndDate.Should().Be(EndOfMonth(2026, 8));
        var exemption = await GetExemptionAsync(factory, planId);
        exemption.IsRevoked.Should().BeTrue();
        exemption.RevokedByFonbecUserId.Should().Be(UserId);
        exemption.RevokedOnUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateSponsorshipAsync_Keeps_Exemption_When_Another_Sponsorship_Covers_Plan()
    {
        var factory = CreateDbContextFactory();
        await SeedStudentAsync(factory);
        var sponsorshipId = await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 12));
        await SeedSponsorshipAsync(
            factory,
            January(2026),
            EndOfMonth(2026, 12),
            sponsorId: OtherSponsorId);
        var planId = await SeedExemptionAsync(factory, new DateTime(2026, 9, 1));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.UpdateSponsorshipAsync(
            UpdateInput(sponsorshipId, January(2026), EndOfMonth(2026, 8)));

        result.Outcome.Should().Be(UpdateSponsorshipOutcome.Saved);
        (await GetExemptionAsync(factory, planId)).IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Rejects_Newly_Covered_Completed_Plan()
    {
        var factory = CreateDbContextFactory();
        await SeedStudentAsync(factory);
        await SeedCompletedPlanAsync(factory, new DateTime(2026, 9, 1));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.CreateSponsorshipAsync(
            SponsorInput(January(2026), EndOfMonth(2026, 9)));

        result.AffectedRows.Should().Be(0);
        result.CompletedPlanStartsOn.Should().Equal(new DateTime(2026, 9, 1));
        (await GetActiveSponsorshipsAsync(factory)).Should().BeEmpty();
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Allows_Covering_Incomplete_Plan()
    {
        var factory = CreateDbContextFactory();
        await SeedStudentAsync(factory);
        await SeedPlanAsync(factory, new DateTime(2026, 9, 1), completed: false);
        var repository = new SponsorshipRepository(factory);

        var result = await repository.CreateSponsorshipAsync(
            SponsorInput(January(2026), EndOfMonth(2026, 9)));

        result.AffectedRows.Should().BeGreaterThan(0);
        result.CompletedPlanStartsOn.Should().BeNull();
    }

    [Fact]
    public async Task UpdateSponsorshipAsync_Rejects_Newly_Covered_Completed_Plan()
    {
        var factory = CreateDbContextFactory();
        await SeedStudentAsync(factory);
        await SeedCompletedPlanAsync(factory, new DateTime(2026, 9, 1));
        var sponsorshipId = await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 6));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.UpdateSponsorshipAsync(
            UpdateInput(sponsorshipId, January(2026), EndOfMonth(2026, 9)));

        result.Outcome.Should().Be(UpdateSponsorshipOutcome.AddsSlotToCompletedPlan);
        result.CompletedPlanStartsOn.Should().Equal(new DateTime(2026, 9, 1));
    }

    [Fact]
    public async Task UpdateSponsorshipAsync_Allows_Period_That_Already_Covered_Completed_Plan()
    {
        var factory = CreateDbContextFactory();
        await SeedStudentAsync(factory);
        await SeedCompletedPlanAsync(factory, new DateTime(2026, 3, 1));
        var sponsorshipId = await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 6));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.UpdateSponsorshipAsync(
            UpdateInput(sponsorshipId, January(2026), EndOfMonth(2026, 8)));

        result.Outcome.Should().Be(UpdateSponsorshipOutcome.Saved);
    }

    private static TestDbContextFactory CreateDbContextFactory() =>
        new(Guid.NewGuid().ToString());

    private static async Task SeedStudentAsync(IDbContextFactory<FonbecWebDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        db.Users.Add(new FonbecWebUser
        {
            Id = UserId,
            UserName = "manager",
            NormalizedUserName = "MANAGER",
            Email = "manager@fonbec.test",
            NormalizedEmail = "MANAGER@FONBEC.TEST",
            FirstName = "Manager",
            LastName = "Test",
            SecurityStamp = Guid.NewGuid().ToString(),
        });
        db.Set<Sponsor>().AddRange(
            new Sponsor
            {
                Id = SponsorId,
                FirstName = "Carlos",
                LastName = "Padrino",
                Email = "carlos@fonbec.test",
                ChapterId = 1,
                CreatedById = UserId,
            },
            new Sponsor
            {
                Id = OtherSponsorId,
                FirstName = "Elena",
                LastName = "Padrina",
                Email = "elena@fonbec.test",
                ChapterId = 1,
                CreatedById = UserId,
            });
        db.Set<Student>().Add(new Student
        {
            Id = StudentId,
            FirstName = "Ana",
            LastName = "Becaria",
            ChapterId = 1,
            FacilitatorId = UserId,
            CreatedById = UserId,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<int> SeedExemptionAsync(
        IDbContextFactory<FonbecWebDbContext> factory,
        DateTime planStartsOn)
    {
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var plan = new PlannedDelivery
        {
            ChapterId = 1,
            StartsOn = planStartsOn,
            CreatedById = UserId,
        };
        db.Set<PlannedDelivery>().Add(plan);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<LetterExemption>().Add(new LetterExemption
        {
            StudentId = StudentId,
            PlannedDeliveryId = plan.Id,
            ChapterId = 1,
            Reason = "Exención de prueba",
            CreatedByFonbecUserId = UserId,
            CreatedOnUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return plan.Id;
    }

    private static Task SeedCompletedPlanAsync(
        IDbContextFactory<FonbecWebDbContext> factory,
        DateTime planStartsOn) =>
        SeedPlanAsync(factory, planStartsOn, completed: true);

    private static async Task SeedPlanAsync(
        IDbContextFactory<FonbecWebDbContext> factory,
        DateTime planStartsOn,
        bool completed)
    {
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        db.Set<PlannedDelivery>().Add(new PlannedDelivery
        {
            ChapterId = 1,
            StartsOn = planStartsOn,
            Completed = completed,
            CreatedById = UserId,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<LetterExemption> GetExemptionAsync(
        IDbContextFactory<FonbecWebDbContext> factory,
        int planId)
    {
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        return await db.Set<LetterExemption>()
            .AsNoTracking()
            .SingleAsync(
                e => e.StudentId == StudentId && e.PlannedDeliveryId == planId,
                TestContext.Current.CancellationToken);
    }

    private static CreateSponsorshipInputDataModel SponsorInput(
        DateTime startDate,
        DateTime? endDate,
        int sponsorId = SponsorId) =>
        new()
        {
            StudentId = StudentId,
            SponsorId = sponsorId,
            SponsorshipStartDate = startDate,
            SponsorshipEndDate = endDate,
            CreatedById = UserId,
        };

    private static CreateSponsorshipInputDataModel CompanyInput(
        DateTime startDate,
        DateTime? endDate) =>
        new()
        {
            StudentId = StudentId,
            CompanyId = CompanyId,
            SponsorshipStartDate = startDate,
            SponsorshipEndDate = endDate,
            CreatedById = UserId,
        };

    private static UpdateSponsorshipInputDataModel UpdateInput(
        int sponsorshipId,
        DateTime startDate,
        DateTime? endDate) =>
        new()
        {
            SponsorshipId = sponsorshipId,
            SponsorshipStartDate = startDate,
            SponsorshipEndDate = endDate,
            UpdatedById = UserId,
        };

    private static async Task<int> SeedSponsorshipAsync(
        IDbContextFactory<FonbecWebDbContext> factory,
        DateTime startDate,
        DateTime? endDate,
        int? sponsorId = SponsorId,
        int? companyId = null,
        string? notes = null)
    {
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var sponsorship = new Sponsorship
        {
            StudentId = StudentId,
            SponsorId = sponsorId,
            CompanyId = companyId,
            StartDate = startDate,
            EndDate = endDate,
            Notes = notes,
            CreatedById = UserId,
        };
        db.Set<Sponsorship>().Add(sponsorship);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return sponsorship.Id;
    }

    private static async Task SeedSharedLetterAsync(
        IDbContextFactory<FonbecWebDbContext> factory,
        DateTime planStartsOn) =>
        await SeedLetterAsync(factory, planStartsOn, SponsorId, companyId: null, shared: true);

    private static async Task SeedUnsharedLetterAsync(
        IDbContextFactory<FonbecWebDbContext> factory,
        DateTime planStartsOn) =>
        await SeedLetterAsync(factory, planStartsOn, SponsorId, companyId: null, shared: false);

    private static async Task SeedSharedCompanyLetterAsync(
        IDbContextFactory<FonbecWebDbContext> factory,
        DateTime planStartsOn,
        int fanOutSponsorId) =>
        await SeedLetterAsync(factory, planStartsOn, sponsorId: null, companyId: CompanyId, shared: true, fanOutSponsorId);

    private static async Task SeedLetterAsync(
        IDbContextFactory<FonbecWebDbContext> factory,
        DateTime planStartsOn,
        int? sponsorId,
        int? companyId,
        bool shared,
        int? fanOutSponsorId = null)
    {
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var plan = new PlannedDelivery
        {
            StartsOn = planStartsOn,
            CreatedById = UserId,
        };
        db.Set<PlannedDelivery>().Add(plan);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var letter = new Letter
        {
            DocumentType = DocumentType.Letter,
            ChapterId = 1,
            StudentId = StudentId,
            SponsorId = sponsorId,
            CompanyId = companyId,
            PlanId = plan.Id,
            FileKind = FileKind.Blob,
            UploadedOn = DateTime.UtcNow,
            UploadedById = UserId,
            Status = DocumentStatus.Approved,
            RowVersion = [1, 0, 0, 0, 0, 0, 0, 0],
        };
        db.Set<Letter>().Add(letter);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        if (shared)
        {
            db.Set<DocumentShare>().Add(new DocumentShare
            {
                DocumentId = letter.DocumentId,
                SponsorId = sponsorId ?? fanOutSponsorId,
                CompanyId = sponsorId is null ? companyId : null,
                StudentId = StudentId,
                SharedOn = DateTime.UtcNow,
                SharedById = UserId,
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
    }

    private static async Task<List<Sponsorship>> GetActiveSponsorshipsAsync(
        IDbContextFactory<FonbecWebDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        return await db.Set<Sponsorship>()
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.StartDate)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private static DateTime January(int year) => new(year, 1, 1);

    private static DateTime EndOfMonth(int year, int month) =>
        new(year, month, DateTime.DaysInMonth(year, month));

    private sealed class TestDbContextFactory(string databaseName)
        : IDbContextFactory<FonbecWebDbContext>
    {
        public FonbecWebDbContext CreateDbContext() => new(CreateOptions());

        public Task<FonbecWebDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());

        private DbContextOptions<FonbecWebDbContext> CreateOptions() =>
            new DbContextOptionsBuilder<FonbecWebDbContext>()
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
    }
}