using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IFacilitatorRepository
{
    Task<List<FacilitatorStudentsDataModel>> GetActiveSponsoredStudentsAsync(int facilitatorId);

    Task<MisBecariosDashboardDataModel> GetMisBecariosDashboardAsync(int facilitatorId);
}

public class FacilitatorRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : IFacilitatorRepository
{
    public async Task<List<FacilitatorStudentsDataModel>> GetActiveSponsoredStudentsAsync(int facilitatorId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var utcNow = DateTime.UtcNow;

        var students = await db.Students
            .Include(s => s.Facilitator)
            .Include(s => s.CreatedBy)
            .Include(s => s.LastUpdatedBy)
            .Include(s => s.DisabledBy)
            .Include(s => s.ReenabledBy)
            .Where(s => s.FacilitatorId == facilitatorId
                        && s.IsActive
                        && !s.IsDeleted
                        && s.Sponsorships.Any(sp =>
                            sp.IsActive
                            && sp.StartDate <= utcNow
                            && (sp.EndDate == null || sp.EndDate >= utcNow)
                            && (
                                (sp.SponsorId != null
                                 && sp.Sponsor != null
                                 && sp.Sponsor.IsActive
                                 && !sp.Sponsor.IsDeleted)
                                || (sp.CompanyId != null
                                    && sp.Company != null
                                    && sp.Company.IsActive))))
            .Select(s => new FacilitatorStudentsDataModel(s)
            {
                StudentId = s.Id,
                StudentFirstName = s.FirstName,
                StudentLastName = s.LastName,
                StudentNickName = s.NickName,
            })
            .OrderBy(s => s.StudentFirstName)
            .ThenBy(s => s.StudentLastName)
            .ToListAsync();

        return students;
    }

    public async Task<MisBecariosDashboardDataModel> GetMisBecariosDashboardAsync(int facilitatorId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var utcNow = DateTime.UtcNow;

        var studentRows = await db.Students
            .AsNoTracking()
            .Where(s => s.FacilitatorId == facilitatorId
                        && s.IsActive
                        && !s.IsDeleted
                        && s.Sponsorships.Any(sp =>
                            sp.IsActive
                            && sp.StartDate <= utcNow
                            && (sp.EndDate == null || sp.EndDate >= utcNow)
                            && (
                                (sp.SponsorId != null
                                 && sp.Sponsor != null
                                 && sp.Sponsor.IsActive
                                 && !sp.Sponsor.IsDeleted)
                                || (sp.CompanyId != null
                                    && sp.Company != null
                                    && sp.Company.IsActive))))
            .Select(s => new
            {
                s.Id,
                s.FirstName,
                s.LastName,
                s.NickName,
                s.SecondarySchoolStartYear,
                s.UniversityStartYear,
                ActiveSponsorships = s.Sponsorships
                    .Where(sp => sp.IsActive
                                 && sp.StartDate <= utcNow
                                 && (sp.EndDate == null || sp.EndDate >= utcNow))
                    .Select(sp => new DashboardSponsorDataModel
                    {
                        SponsorshipId = sp.Id,
                        SponsorId = sp.SponsorId,
                        CompanyId = sp.CompanyId,
                        RecipientName = sp.CompanyId != null && sp.Company != null
                            ? sp.Company.Name
                            : sp.Sponsor != null
                                ? sp.Sponsor.FirstName + " " + sp.Sponsor.LastName
                                : string.Empty,
                        IsCompany = sp.CompanyId != null,
                    })
                    .ToList(),
            })
            .OrderBy(s => s.FirstName)
            .ThenBy(s => s.LastName)
            .ToListAsync();

        var students = studentRows.Select(s => new MisBecariosRowDataModel
        {
            StudentId = s.Id,
            StudentFirstName = s.FirstName,
            StudentLastName = s.LastName,
            StudentNickName = s.NickName,
            EducationLevel = ComputeEducationLevel(
                s.SecondarySchoolStartYear, s.UniversityStartYear, utcNow),
            Sponsors = s.ActiveSponsorships,
        }).ToList();

        return new MisBecariosDashboardDataModel
        {
            Students = students,
        };
    }

    private static EducationLevel ComputeEducationLevel(
        DateTime? secondarySchoolStartYear,
        DateTime? universityStartYear,
        DateTime utcNow)
    {
        if (universityStartYear is not null && universityStartYear <= utcNow)
        {
            return EducationLevel.University;
        }

        if (secondarySchoolStartYear is not null && secondarySchoolStartYear <= utcNow)
        {
            return EducationLevel.SecondarySchool;
        }

        return EducationLevel.PrimarySchool;
    }
}