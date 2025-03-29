using Fonbec.Web.DataAccess.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess;

public class FonbecWebDbContext(DbContextOptions<FonbecWebDbContext> options) : IdentityDbContext<FonbecWebUser>(options)
{
}