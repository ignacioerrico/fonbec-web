using System;
using System.Collections.Generic;
using System.Text;
using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface ICompanyRepository
{
    Task<int> CreateCompanyAsync(CreateCompanyInputDataModel dataModel);
    Task<string?> GetCompanyByNameAsync(string companyName);
}
public class CompanyRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : ICompanyRepository
{
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
        return await db.SaveChangesAsync();
    }

    public async Task<string?> GetCompanyByNameAsync(string companyName)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.Companies
            .Where(x => x.Name == companyName)
            .Select(x => x.Name)
            .FirstOrDefaultAsync();
    }
}

