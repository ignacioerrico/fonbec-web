using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IFacilitatorRepository
{
    Task<List<FacilitatorStudentsDataModel>> GetActiveSponsoredStudentsAsync(int facilitatorId);
}

public class FacilitatorRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : IFacilitatorRepository
{
    public async Task<List<FacilitatorStudentsDataModel>> GetActiveSponsoredStudentsAsync(int facilitatorId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var utcNow = DateTime.UtcNow;

        var students = await db.Students
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
            .Select(s => new FacilitatorStudentsDataModel
            {
                StudentId = s.Id,
                StudentFirstName = s.FirstName,
                StudentLastName = s.LastName,
                StudentNickName = s.NickName,
                EducationLevel = s.SecondarySchoolStartYear == null && s.UniversityStartYear == null
                    ? EducationLevel.PrimarySchool
                    : s.UniversityStartYear != null && s.UniversityStartYear <= utcNow
                        ? EducationLevel.University
                        : s.SecondarySchoolStartYear != null && s.SecondarySchoolStartYear <= utcNow
                            ? EducationLevel.SecondarySchool
                            : EducationLevel.PrimarySchool,
                Sponsors = s.Sponsorships
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
            .OrderBy(s => s.StudentFirstName)
            .ThenBy(s => s.StudentLastName)
            .ToListAsync();

        return students;
    }
}
