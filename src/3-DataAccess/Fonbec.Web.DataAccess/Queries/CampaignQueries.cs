using System.Linq.Expressions;
using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Queries;

/// <summary>
/// The open campaign and the sponsorships that belong to it.
/// Callers project their own rows; the filters live only here so a facilitator list,
/// letter-plan progress, and uploads stay on the same plan and the same students.
/// </summary>
public static class CampaignQueries
{
    /// <summary>Active planned deliveries that have not been completed.</summary>
    public static IQueryable<PlannedDelivery> Open(IQueryable<PlannedDelivery> plans) =>
        plans.Where(pd => pd.IsActive && !pd.Completed);

    /// <summary>
    /// The chapter's open plans, most recently started first. The first row is the current plan,
    /// including one created for a month that has not started yet.
    /// </summary>
    public static IQueryable<PlannedDelivery> OpenForChapter(IQueryable<PlannedDelivery> plans, int? chapterId) =>
        Open(plans)
            .Where(pd => pd.ChapterId == chapterId)
            .OrderByDescending(pd => pd.StartsOn);

    public static Task<CurrentPlanDataModel?> SelectCurrentAsync(IQueryable<PlannedDelivery> openPlans) =>
        openPlans
            .Select(pd => new CurrentPlanDataModel
            {
                PlanId = pd.Id,
                StartsOn = pd.StartsOn,
            })
            .FirstOrDefaultAsync();

    public static Task<DateTime?> SelectStartsOnAsync(IQueryable<PlannedDelivery> openPlans) =>
        openPlans
            .Select(pd => (DateTime?)pd.StartsOn)
            .FirstOrDefaultAsync();

    /// <summary>
    /// A sponsorship in effect on <paramref name="asOf"/> whose sponsor or company is still active.
    /// Pass a new expression into each query operator; EF cannot share one expression instance.
    /// </summary>
    public static Expression<Func<Sponsorship, bool>> Covers(DateTime asOf) =>
        sp => sp.IsActive
              && sp.StartDate <= asOf
              && (sp.EndDate == null || sp.EndDate >= asOf)
              && ((sp.SponsorId != null
                   && sp.Sponsor != null
                   && sp.Sponsor.IsActive
                   && !sp.Sponsor.IsDeleted)
                  || (sp.CompanyId != null
                      && sp.Company != null
                      && sp.Company.IsActive));

    public static IQueryable<Student> WhereCovered(
        this IQueryable<Student> students,
        Expression<Func<Sponsorship, bool>> covers) =>
        students.Where(s => s.IsActive
                            && !s.IsDeleted
                            && s.Sponsorships.AsQueryable().Any(covers));
}