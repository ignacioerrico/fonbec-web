using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class CompanyRepositoryUpdateTests
{
    [Fact]
    public async Task CompanyNameExistsAsync_Returns_False_When_Only_Excluded_Company_Has_Name()
    {
        var factory = CreateDbContextFactory();
        await SeedCompaniesAsync(factory, ("Acme Corp", 1), ("Other Corp", 2));
        var repository = new CompanyRepository(factory);

        var exists = await repository.CompanyNameExistsAsync("Acme Corp", excludeCompanyId: 1);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task CompanyNameExistsAsync_Returns_True_When_Another_Company_Has_Name()
    {
        var factory = CreateDbContextFactory();
        await SeedCompaniesAsync(factory, ("Acme Corp", 1), ("Other Corp", 2));
        var repository = new CompanyRepository(factory);

        var exists = await repository.CompanyNameExistsAsync("Other Corp", excludeCompanyId: 1);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCompanyAsync_Updates_Company_When_Name_Is_Unique()
    {
        var factory = CreateDbContextFactory();
        await SeedCompaniesAsync(factory, ("Acme Corp", 1), ("Other Corp", 2));
        var repository = new CompanyRepository(factory);

        var affectedRows = await repository.UpdateCompanyAsync(new UpdateCompanyInputDataModel
        {
            CompanyId = 1,
            CompanyUpdatedName = "Renamed Corp",
            CompanyUpdatedEmail = "new@acme.test",
            CompanyUpdatedPhoneNumber = "555",
            CompanyUpdatedNotes = "Updated",
            UpdatedById = 1,
        });

        affectedRows.Should().Be(1);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var company = await db.Set<Company>()
            .SingleAsync(c => c.Id == 1, TestContext.Current.CancellationToken);
        company.Name.Should().Be("Renamed Corp");
        company.Email.Should().Be("new@acme.test");
    }

    private static TestDbContextFactory CreateDbContextFactory() =>
        new(Guid.NewGuid().ToString());

    private static async Task SeedCompaniesAsync(
        TestDbContextFactory factory,
        params (string Name, int Id)[] companies)
    {
        await using var db = await factory.CreateDbContextAsync();

        db.Users.Add(new FonbecWebUser
        {
            Id = 1,
            UserName = "manager",
            NormalizedUserName = "MANAGER",
            Email = "manager@fonbec.test",
            NormalizedEmail = "MANAGER@FONBEC.TEST",
            FirstName = "Manager",
            LastName = "User",
            SecurityStamp = Guid.NewGuid().ToString(),
        });

        foreach (var (name, id) in companies)
        {
            db.Set<Company>().Add(new Company
            {
                Id = id,
                Name = name,
                CreatedById = 1,
                CreatedOnUtc = DateTime.UtcNow,
                IsActive = true,
            });
        }

        await db.SaveChangesAsync();
    }

    private sealed class TestDbContextFactory(string databaseName) : IDbContextFactory<FonbecWebDbContext>
    {
        public FonbecWebDbContext CreateDbContext() =>
            new(CreateOptions());

        public Task<FonbecWebDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());

        private DbContextOptions<FonbecWebDbContext> CreateOptions() =>
            new DbContextOptionsBuilder<FonbecWebDbContext>()
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
    }
}
