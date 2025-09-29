using Microsoft.AspNetCore.Authorization;

namespace Fonbec.Web.Ui.Authorization;

internal class FonbecPermissionRequirement(string pageName) : IAuthorizationRequirement
{
    public string PageName { get; } = pageName;
}