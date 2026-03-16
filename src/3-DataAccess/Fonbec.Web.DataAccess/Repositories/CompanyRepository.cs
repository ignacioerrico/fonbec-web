using Fonbec.Web.DataAccess.DataModels.Companies;
using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface ICompanyRepository
{
    Task<List<AllCompaniesDataModel>> GetAllCompaniesAsync();
    Task<bool> CompanyNameExistsAsync(string companyName);
    Task<int> CreateCompanyAsync(CreateCompanyInputDataModel dataModel);
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
            .Where(c => c.IsActive)
            .Select(c => new AllCompaniesDataModel
            {
                CompanyId = c.Id,
                CompanyName = c.Name,
                CompanyPhoneNumber = c.PhoneNumber,
                CompanyEmail = c.Email,
            })
            .OrderBy(c => c.CompanyName)
            .ToListAsync();

        return allCompanies;
    }

    public async Task<bool> CompanyNameExistsAsync(string companyName)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var nameExists = await db.Companies
                .AnyAsync(c => c.Name == companyName);

        return nameExists;
    }

    public async Task<int> CreateCompanyAsync(CreateCompanyInputDataModel dataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var company = new Company
        {
            Name = dataModel.CompanyName,
            PhoneNumber = dataModel.CompanyPhoneNumber,
            Email = dataModel.CompanyEmail,
            CreatedById = dataModel.CreatedById,
        };

        db.Companies.Add(company);
        await db.SaveChangesAsync();

        return company.Id;
    }
}