using Fonbec.Web.DataAccess.DataModels.Companies;
using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface ICompanyRepository
{
    Task<List<AllCompaniesDataModel>> GetAllCompaniesAsync();
    Task<bool> CompanyNameExistsAsync(string companyName, int? excludeCompanyId = null);
    Task<CreateCompanyRepositoryResult> CreateCompanyAsync(CreateCompanyInputDataModel dataModel);
    Task<int> UpdateCompanyAsync(UpdateCompanyInputDataModel dataModel);
}

public class CompanyRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : ICompanyRepository
{
    public async Task<List<AllCompaniesDataModel>> GetAllCompaniesAsync()
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var allCompanies = await db.Companies
            .Include(c => c.CreatedBy)
            .Include(c => c.LastUpdatedBy)
            .Include(c => c.DisabledBy)
            .Include(c => c.ReenabledBy)
            .Include(c => c.PointsOfContact)
            .Include(c => c.Sponsors)
            .Where(c => c.IsActive)
            .Select(c => new AllCompaniesDataModel(c)
            {
                CompanyId = c.Id,
                CompanyName = c.Name,
                CompanyPhoneNumber = c.PhoneNumber,
                CompanyEmail = c.Email,
                CompanySponsors = c.Sponsors,
                CompanyPointsOfContact = c.PointsOfContact
            })
            .OrderBy(c => c.CompanyName)
            .ToListAsync();

        return allCompanies;
    }

    public async Task<bool> CompanyNameExistsAsync(string companyName, int? excludeCompanyId = null)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var nameExists = await db.Companies
            .AnyAsync(c => c.Name == companyName && (!excludeCompanyId.HasValue || c.Id != excludeCompanyId.Value));

        return nameExists;
    }

    public async Task<CreateCompanyRepositoryResult> CreateCompanyAsync(CreateCompanyInputDataModel dataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        List<Sponsor>? sponsorsToLink = null;

        if (dataModel.SponsorIds.Count > 0)
        {
            var sponsorIds = dataModel.SponsorIds.Distinct().ToList();

            var sponsors = await db.Sponsors
                .Where(s => sponsorIds.Contains(s.Id) && !s.IsDeleted && s.IsActive && s.CompanyId == null)
                .ToListAsync();

            var foundIds = sponsors.Select(s => s.Id).ToHashSet();
            var unavailableIds = sponsorIds.Where(id => !foundIds.Contains(id))
                .ToList();
            if (unavailableIds.Count > 0)
            {
                return new CreateCompanyRepositoryResult(MissingSponsorIds: unavailableIds);
            }

            sponsorsToLink = sponsors;
        }

        var company = new Company
        {
            Name = dataModel.CompanyName,
            PhoneNumber = dataModel.CompanyPhoneNumber,
            Email = dataModel.CompanyEmail,
            Notes = dataModel.CompanyNotes,
            PointsOfContact = dataModel.PointsOfContact.Select(poc =>
                new PointOfContact
                {
                    FirstName = poc.PocFirstName,
                    LastName = poc.PocLastName,
                    NickName = poc.PocNickName,
                    Email = poc.PocEmail,
                    PhoneNumber = poc.PocPhoneNumber,
                    Notes = poc.PocNotes,
                    CreatedById = dataModel.CreatedById,
                }).ToList(),
            CreatedById = dataModel.CreatedById,
        };

        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            db.Companies.Add(company);

            if (sponsorsToLink is not null)
            {
                foreach (var sponsor in sponsorsToLink)
                {
                    sponsor.Company = company;
                }
            }

            var affectedRows = await db.SaveChangesAsync();

            if (affectedRows == 0)
            {
                await transaction.RollbackAsync();
                return new CreateCompanyRepositoryResult();
            }

            await transaction.CommitAsync();
            return new CreateCompanyRepositoryResult(company.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<int> UpdateCompanyAsync(UpdateCompanyInputDataModel dataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var companyDb = await db.Companies.FindAsync(dataModel.CompanyId);

        if (companyDb is not { IsActive: true })
        {
            return 0;
        }

        companyDb.Name = dataModel.CompanyUpdatedName;
        companyDb.Email = dataModel.CompanyUpdatedEmail;
        companyDb.PhoneNumber = dataModel.CompanyUpdatedPhoneNumber;
        companyDb.Notes = dataModel.CompanyUpdatedNotes;
        companyDb.LastUpdatedById = dataModel.UpdatedById;

        db.Companies.Update(companyDb);
        return await db.SaveChangesAsync();
    }
}