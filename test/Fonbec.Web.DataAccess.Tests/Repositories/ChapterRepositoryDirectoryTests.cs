using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class ChapterRepositoryDirectoryTests
{
    [Fact]
    public async Task GetChapterDirectoryAsync_GroupsStaff_FlagsLinkedFacilitators_AndCountsChapter()
    {
        var databaseName = Guid.NewGuid().ToString();
        var factory = new TestDbContextFactory(databaseName);
        var repository = new ChapterRepository(factory);

        const int activeChapterId = 1;
        const int otherChapterId = 2;
        const int inactiveChapterId = 3;

        const int managerId = 10;
        const int inactiveManagerId = 11;
        const int facilitatorWithStudentId = 12;
        const int facilitatorWithoutStudentsId = 13;
        const int reviewerId = 14;
        const int adminId = 15;
        const int otherChapterManagerId = 16;
        const int inactiveChapterManagerId = 17;
        const int globalManagerId = 18;

        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Roles.AddRange(
                Role(1, FonbecRole.Manager),
                Role(2, FonbecRole.Uploader),
                Role(3, FonbecRole.Reviewer),
                Role(4, FonbecRole.Admin));

            db.Set<Chapter>().AddRange(
                new Chapter { Id = activeChapterId, Name = "Norte", CreatedById = managerId },
                new Chapter { Id = otherChapterId, Name = "Sur", CreatedById = managerId },
                new Chapter { Id = inactiveChapterId, Name = "Cerrada", CreatedById = managerId });

            db.Users.AddRange(
                User(managerId, activeChapterId, "Ana", "Zapata", "ana@fonbec.org", "111", nickName: "Anita"),
                User(inactiveManagerId, activeChapterId, "Inactivo", "Perez", "off@fonbec.org", "222", lockedOut: true),
                User(facilitatorWithStudentId, activeChapterId, "Mara", "Diaz", "mara@fonbec.org", null),
                User(facilitatorWithoutStudentsId, activeChapterId, "Nico", "Bazan", "nico@fonbec.org", "444"),
                User(reviewerId, activeChapterId, "Eva", "Ruiz", "eva@fonbec.org", "555"),
                User(adminId, activeChapterId, "Global", "Admin", "admin@fonbec.org", "666"),
                User(otherChapterManagerId, otherChapterId, "Luis", "Alvarez", "luis@fonbec.org", "777"),
                User(inactiveChapterManagerId, inactiveChapterId, "Cerrado", "Gomez", "cerrado@fonbec.org", "888"),
                User(globalManagerId, null, "Sin", "Filial", "global@fonbec.org", "999"));

            db.UserRoles.AddRange(
                new IdentityUserRole<int> { UserId = managerId, RoleId = 1 },
                new IdentityUserRole<int> { UserId = inactiveManagerId, RoleId = 1 },
                new IdentityUserRole<int> { UserId = facilitatorWithStudentId, RoleId = 2 },
                new IdentityUserRole<int> { UserId = facilitatorWithoutStudentsId, RoleId = 2 },
                new IdentityUserRole<int> { UserId = reviewerId, RoleId = 3 },
                new IdentityUserRole<int> { UserId = adminId, RoleId = 4 },
                new IdentityUserRole<int> { UserId = otherChapterManagerId, RoleId = 1 },
                new IdentityUserRole<int> { UserId = inactiveChapterManagerId, RoleId = 1 },
                new IdentityUserRole<int> { UserId = globalManagerId, RoleId = 1 });

            db.Set<Student>().AddRange(
                new Student
                {
                    Id = 1,
                    FirstName = "Activo",
                    LastName = "Becario",
                    Gender = Gender.Unknown,
                    ChapterId = activeChapterId,
                    FacilitatorId = managerId,
                    CreatedById = managerId,
                },
                new Student
                {
                    Id = 2,
                    FirstName = "Inactivo",
                    LastName = "Becario",
                    Gender = Gender.Unknown,
                    ChapterId = activeChapterId,
                    FacilitatorId = facilitatorWithStudentId,
                    CreatedById = managerId,
                    IsActive = false,
                },
                new Student
                {
                    Id = 3,
                    FirstName = "Sur",
                    LastName = "Becario",
                    Gender = Gender.Unknown,
                    ChapterId = otherChapterId,
                    FacilitatorId = otherChapterManagerId,
                    CreatedById = managerId,
                });

            db.Set<Sponsor>().AddRange(
                Sponsor(1, activeChapterId, "Padrino", "Activo"),
                Sponsor(2, activeChapterId, "Padrino", "Inactivo"),
                Sponsor(3, activeChapterId, "Padrino", "Borrado", isDeleted: true),
                Sponsor(4, otherChapterId, "Padrino", "Sur"));

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var inactiveChapter = await db.Set<Chapter>().SingleAsync(
                chapter => chapter.Id == inactiveChapterId,
                TestContext.Current.CancellationToken);
            inactiveChapter.DisabledById = managerId;

            var inactiveStudent = await db.Set<Student>().SingleAsync(
                student => student.Id == 2,
                TestContext.Current.CancellationToken);
            inactiveStudent.DisabledById = managerId;

            var inactiveSponsor = await db.Set<Sponsor>().SingleAsync(
                sponsor => sponsor.Id == 2,
                TestContext.Current.CancellationToken);
            inactiveSponsor.DisabledById = managerId;

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            inactiveStudent.IsActive.Should().BeFalse();
            inactiveSponsor.IsActive.Should().BeFalse();
            inactiveSponsor.IsDeleted.Should().BeFalse();
            inactiveChapter.IsActive.Should().BeFalse();
        }

        var directory = await repository.GetChapterDirectoryAsync();

        directory.Staff.Select(member => member.UserId).Should().BeEquivalentTo(
        [
            managerId,
            inactiveManagerId,
            facilitatorWithStudentId,
            facilitatorWithoutStudentsId,
            reviewerId,
            otherChapterManagerId,
        ]);

        var north = directory.Staff.Where(member => member.ChapterId == activeChapterId).ToList();
        north.Where(member => member.Role == FonbecRole.Manager).Select(member => member.UserId)
            .Should().BeEquivalentTo([managerId, inactiveManagerId]);
        north.Where(member => member.Role == FonbecRole.Uploader).Select(member => member.UserId)
            .Should().BeEquivalentTo([facilitatorWithStudentId, facilitatorWithoutStudentsId]);
        north.Should().ContainSingle(member => member.Role == FonbecRole.Reviewer)
            .Which.UserId.Should().Be(reviewerId);

        var activeManager = north.Single(member => member.UserId == managerId);
        activeManager.FirstName.Should().Be("Ana");
        activeManager.LastName.Should().Be("Zapata");
        activeManager.Email.Should().Be("ana@fonbec.org");
        activeManager.PhoneNumber.Should().Be("111");
        activeManager.NickName.Should().Be("Anita");
        activeManager.IsActive.Should().BeTrue();
        activeManager.CanBeLockedOut.Should().BeTrue();
        activeManager.HasLinkedStudents.Should().BeFalse();

        var inactiveManager = north.Single(member => member.UserId == inactiveManagerId);
        inactiveManager.NickName.Should().BeNull();
        inactiveManager.IsActive.Should().BeFalse();
        inactiveManager.CanBeLockedOut.Should().BeTrue();

        north.Single(member => member.UserId == facilitatorWithStudentId).HasLinkedStudents.Should().BeTrue();
        north.Single(member => member.UserId == facilitatorWithoutStudentsId).HasLinkedStudents.Should().BeFalse();

        directory.Staff.Should().ContainSingle(member => member.ChapterId == otherChapterId)
            .Which.UserId.Should().Be(otherChapterManagerId);

        directory.StudentCounts[activeChapterId].Should().Be(1);
        directory.StudentCounts[otherChapterId].Should().Be(1);
        directory.SponsorCounts[activeChapterId].Should().Be(2);
        directory.SponsorCounts[otherChapterId].Should().Be(1);
        directory.StudentCounts.Should().NotContainKey(inactiveChapterId);
        directory.SponsorCounts.Should().NotContainKey(inactiveChapterId);
    }

    private static FonbecWebRole Role(int id, string name) =>
        new()
        {
            Id = id,
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
        };

    private static FonbecWebUser User(
        int id,
        int? chapterId,
        string firstName,
        string lastName,
        string email,
        string? phone,
        bool lockedOut = false,
        string? nickName = null) =>
        new()
        {
            Id = id,
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            NickName = nickName,
            PhoneNumber = phone,
            ChapterId = chapterId,
            LockoutEnabled = true,
            LockoutEnd = lockedOut ? DateTimeOffset.MaxValue : null,
        };

    private static Sponsor Sponsor(int id, int chapterId, string firstName, string lastName, bool isDeleted = false) =>
        new()
        {
            Id = id,
            ChapterId = chapterId,
            FirstName = firstName,
            LastName = lastName,
            Gender = Gender.Unknown,
            Email = $"{firstName}.{lastName}@fonbec.org".ToLowerInvariant(),
            CreatedById = 10,
            IsDeleted = isDeleted,
        };

    private sealed class TestDbContextFactory(string databaseName) : IDbContextFactory<FonbecWebDbContext>
    {
        public FonbecWebDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<FonbecWebDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options);

        public Task<FonbecWebDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}