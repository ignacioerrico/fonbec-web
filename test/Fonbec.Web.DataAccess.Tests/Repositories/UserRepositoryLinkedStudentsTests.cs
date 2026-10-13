using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class UserRepositoryLinkedStudentsTests
{
    [Fact]
    public async Task GetUserIdsWithLinkedStudentsAsync_ReturnsOnlyFacilitatorsWithAtLeastOneStudent()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<FonbecWebDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddIdentityCore<FonbecWebUser>()
            .AddRoles<FonbecWebRole>()
            .AddEntityFrameworkStores<FonbecWebDbContext>();

        using var provider = services.BuildServiceProvider();
        var userManager = provider.GetRequiredService<UserManager<FonbecWebUser>>();
        var userStore = provider.GetRequiredService<IUserStore<FonbecWebUser>>();
        var factory = new TestDbContextFactory(databaseName);
        var repository = new UserRepository(userManager, userStore, factory);

        const int facilitatorWithInactiveStudent = 10;
        const int facilitatorWithoutStudents = 20;
        const int facilitatorWithActiveStudent = 30;
        const int facilitatorNotQueried = 40;

        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Set<Student>().Add(new Student
            {
                Id = 1,
                FirstName = "Inactivo",
                LastName = "Becario",
                Gender = Gender.Unknown,
                ChapterId = 1,
                FacilitatorId = facilitatorWithInactiveStudent,
                CreatedById = 1,
                IsActive = false,
            });
            db.Set<Student>().Add(new Student
            {
                Id = 2,
                FirstName = "Activo",
                LastName = "Becario",
                Gender = Gender.Unknown,
                ChapterId = 1,
                FacilitatorId = facilitatorWithActiveStudent,
                CreatedById = 1,
            });
            db.Set<Student>().Add(new Student
            {
                Id = 3,
                FirstName = "Otro",
                LastName = "Becario",
                Gender = Gender.Unknown,
                ChapterId = 1,
                FacilitatorId = facilitatorNotQueried,
                CreatedById = 1,
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var inactiveStudent = await db.Set<Student>().SingleAsync(student => student.Id == 1, TestContext.Current.CancellationToken);
            inactiveStudent.DisabledById = 1;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            inactiveStudent.IsActive.Should().BeFalse();
        }

        var linked = await repository.GetUserIdsWithLinkedStudentsAsync(
        [
            facilitatorWithInactiveStudent,
            facilitatorWithoutStudents,
            facilitatorWithActiveStudent,
        ]);

        linked.Should().BeEquivalentTo([facilitatorWithInactiveStudent, facilitatorWithActiveStudent]);
    }

    [Fact]
    public async Task GetFacilitatorStudentsAsync_ReturnsNonDeletedStudentsForTheRequestedFacilitators()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<FonbecWebDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddIdentityCore<FonbecWebUser>()
            .AddRoles<FonbecWebRole>()
            .AddEntityFrameworkStores<FonbecWebDbContext>();

        using var provider = services.BuildServiceProvider();
        var userManager = provider.GetRequiredService<UserManager<FonbecWebUser>>();
        var userStore = provider.GetRequiredService<IUserStore<FonbecWebUser>>();
        var factory = new TestDbContextFactory(databaseName);
        var repository = new UserRepository(userManager, userStore, factory);

        const int facilitatorId = 10;
        const int otherFacilitatorId = 20;

        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Set<Student>().AddRange(
                new Student
                {
                    Id = 1,
                    FirstName = "Zoe",
                    LastName = "Zulu",
                    Gender = Gender.Unknown,
                    ChapterId = 1,
                    FacilitatorId = facilitatorId,
                    CreatedById = 1,
                },
                new Student
                {
                    Id = 2,
                    FirstName = "Ana",
                    LastName = "Alba",
                    Gender = Gender.Unknown,
                    ChapterId = 1,
                    FacilitatorId = facilitatorId,
                    CreatedById = 1,
                    IsDeleted = true,
                },
                new Student
                {
                    Id = 3,
                    FirstName = "Otro",
                    LastName = "Becario",
                    Gender = Gender.Unknown,
                    ChapterId = 1,
                    FacilitatorId = otherFacilitatorId,
                    CreatedById = 1,
                });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var inactiveStudent = await db.Set<Student>().SingleAsync(student => student.Id == 1, TestContext.Current.CancellationToken);
            inactiveStudent.DisabledById = 1;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var students = await repository.GetFacilitatorStudentsAsync([facilitatorId]);

        students.Should().ContainSingle();
        students[0].FacilitatorId.Should().Be(facilitatorId);
        students[0].FirstName.Should().Be("Zoe");
        students[0].LastName.Should().Be("Zulu");
    }

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