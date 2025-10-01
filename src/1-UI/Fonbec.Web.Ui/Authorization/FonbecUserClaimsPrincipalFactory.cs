using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Fonbec.Web.Ui.Authorization;

public class FonbecUserClaimsPrincipalFactory(
    IChapterService chapterService,
    UserManager<FonbecWebUser> userManager,
    RoleManager<FonbecWebRole> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<FonbecWebUser, FonbecWebRole>(
        userManager,
        roleManager,
        options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(FonbecWebUser user)
    {
        var claimsIdentity = await base.GenerateClaimsAsync(user);

        var claim = new Claim(FonbecWebUserCustomClaim.FirstName, user.FirstName);
        claimsIdentity.AddClaim(claim);

        claim = new Claim(FonbecWebUserCustomClaim.LastName, user.LastName);
        claimsIdentity.AddClaim(claim);

        if (user.NickName is not null)
        {
            claim = new Claim(FonbecWebUserCustomClaim.NickName, user.NickName);
            claimsIdentity.AddClaim(claim);
        }

        if (user.Gender != Gender.Unknown)
        {
            var gender = user.Gender switch
            {
                Gender.Male => nameof(Gender.Male),
                Gender.Female => nameof(Gender.Female),
                _ => nameof(Gender.Unknown)
            };
            claim = new Claim(FonbecWebUserCustomClaim.Gender, gender);
            claimsIdentity.AddClaim(claim);
        }

        if (user.ChapterId is not null)
        {
            var chapterId = user.ChapterId.Value;

            claim = new Claim(FonbecWebUserCustomClaim.ChapterId, chapterId.ToString());
            claimsIdentity.AddClaim(claim);

            var chapterName = await chapterService.GetChapterNameAsync(chapterId);
            
            claim = new Claim(FonbecWebUserCustomClaim.ChapterName, chapterName);
            claimsIdentity.AddClaim(claim);
        }

        return claimsIdentity;
    }
}