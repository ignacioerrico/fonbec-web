using Fonbec.Web.DataAccess.DataModels.Companies;
using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.DataAccess.DataModels.Review;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface ICompanyRepository
{
    Task<List<AllCompaniesDataModel>> GetAllCompaniesAsync();

    /// <summary>Active companies visible on the companies list.</summary>
    Task<int> CountCompaniesAsync();

    Task<bool> CompanyNameExistsAsync(string companyName, int? excludeCompanyId = null);
    Task<CreateCompanyRepositoryResult> CreateCompanyAsync(CreateCompanyInputDataModel dataModel);
    Task<int> UpdateCompanyAsync(UpdateCompanyInputDataModel dataModel);
    Task<CandidateNameDataModel?> GetCompanyNameAsync(int companyId);
    /// <summary>
    /// Get every active company name except <paramref name="excludeCompanyId"/>, ordered by id.
    /// The review picker samples this pool deterministically.
    /// </summary>
    Task<List<CandidateNameDataModel>> GetCompanyCandidateNamesAsync(int? excludeCompanyId);
}

public class CompanyRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : ICompanyRepository
{
    public async Task<List<AllCompaniesDataModel>> GetAllCompaniesAsync()
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var allCompanies = await db.Companies
            .AsNoTracking()
            .Include(c => c.CreatedBy)
            .Include(c => c.LastUpdatedBy)
            .Include(c => c.DisabledBy)
            .Include(c => c.ReenabledBy)
            .Where(c => c.IsActive)
            .Select(c => new AllCompaniesDataModel(c)
            {
                CompanyId = c.Id,
                CompanyName = c.Name,
                CompanyPhoneNumber = c.PhoneNumber,
                CompanyEmail = c.Email,
                CompanySponsors = c.Sponsors == null ? new() : c.Sponsors.Where(s => s.IsActive && !s.IsDeleted).ToList(),
                CompanyPointsOfContact = c.PointsOfContact.Where(p => p.IsActive).ToList()
            })
            .OrderBy(c => c.CompanyName)
            .ToListAsync();

        if (allCompanies.Count == 0)
        {
            return allCompanies;
        }

        var companyIds = allCompanies.Select(c => c.CompanyId).ToList();
        var sponsoredStudents = await GetSponsoredStudentsAsync(db, companyIds);
        foreach (var company in allCompanies)
        {
            company.SponsoredStudents = sponsoredStudents
                .Where(student => student.DirectCompanyId == company.CompanyId)
                .OrderBy(student => student.Name)
                .ThenBy(student => student.StartDate)
                .Select(student => new CompanySponsoredStudentDataModel
                {
                    Name = student.Name,
                    SponsorName = student.SponsorName,
                    StartDate = student.StartDate,
                    EndDate = student.EndDate,
                })
                .ToList();
        }

        return allCompanies;
    }

    private static async Task<List<CompanyStudentRow>> GetSponsoredStudentsAsync(
        FonbecWebDbContext db,
        List<int> companyIds)
    {
        var direct = await db.Sponsorships
            .AsNoTracking()
            .Where(sp => sp.CompanyId != null
                         && companyIds.Contains(sp.CompanyId.Value)
                         && sp.IsActive
                         && sp.Student.IsActive
                         && !sp.Student.IsDeleted)
            .Select(sp => new
            {
                CompanyId = sp.CompanyId,
                Name = sp.Student.FirstName + " " + sp.Student.LastName,
                sp.StartDate,
                sp.EndDate,
            })
            .ToListAsync();

        var throughSponsors = await db.Sponsorships
            .AsNoTracking()
            .Where(sp => sp.Sponsor != null
                         && sp.Sponsor.CompanyId != null
                         && companyIds.Contains(sp.Sponsor.CompanyId.Value)
                         && sp.Sponsor.IsActive
                         && !sp.Sponsor.IsDeleted
                         && sp.IsActive
                         && sp.Student.IsActive
                         && !sp.Student.IsDeleted
                         && (sp.CompanyId == null || sp.CompanyId != sp.Sponsor.CompanyId))
            .Select(sp => new
            {
                CompanyId = sp.Sponsor!.CompanyId,
                SponsorName = sp.Sponsor.FirstName + " " + sp.Sponsor.LastName,
                Name = sp.Student.FirstName + " " + sp.Student.LastName,
                sp.StartDate,
                sp.EndDate,
            })
            .ToListAsync();

        return direct.Select(student => new CompanyStudentRow
            {
                DirectCompanyId = student.CompanyId,
                Name = student.Name,
                StartDate = student.StartDate,
                EndDate = student.EndDate,
            })
            .Concat(throughSponsors.Select(student => new CompanyStudentRow
            {
                DirectCompanyId = student.CompanyId,
                SponsorName = student.SponsorName,
                Name = student.Name,
                StartDate = student.StartDate,
                EndDate = student.EndDate,
            }))
            .ToList();
    }

    private sealed class CompanyStudentRow
    {
        public int? DirectCompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? SponsorName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public async Task<int> CountCompaniesAsync()
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.Companies
            .AsNoTracking()
            .CountAsync(c => c.IsActive);
    }

    public async Task<bool> CompanyNameExistsAsync(string companyName, int? excludeCompanyId = null)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var nameExists = await db.Companies
            .AsNoTracking()
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
            PublicAccessToken = Guid.NewGuid(),
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

    public async Task<CandidateNameDataModel?> GetCompanyNameAsync(int companyId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.Companies
            .AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => new CandidateNameDataModel
            {
                Id = c.Id,
                FirstName = c.Name,
                LastName = string.Empty,
                IsCompany = true,
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<CandidateNameDataModel>> GetCompanyCandidateNamesAsync(int? excludeCompanyId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var query = db.Companies
            .AsNoTracking()
            .Where(c => c.IsActive);

        if (excludeCompanyId.HasValue)
        {
            query = query.Where(c => c.Id != excludeCompanyId.Value);
        }

        return await query
            .OrderBy(c => c.Id)
            .Select(c => new CandidateNameDataModel
            {
                Id = c.Id,
                FirstName = c.Name,
                LastName = string.Empty,
                IsCompany = true,
            })
            .ToListAsync();
    }
}