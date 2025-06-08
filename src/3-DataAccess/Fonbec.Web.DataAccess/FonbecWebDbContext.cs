using Fonbec.Web.DataAccess.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess;

public class FonbecWebDbContext(DbContextOptions<FonbecWebDbContext> options)
    : IdentityDbContext<FonbecWebUser, FonbecWebRole, int>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // The keys of Identity tables are mapped in the `OnModelCreating` method of `IdentityDbContext`.
        // If this method is not called, this error will occur when creating a migration:
        // > Unable to create a 'DbContext' of type 'FonbecWebDbContext'. The exception 'The entity type 'IdentityUserLogin<int>'
        // requires a primary key to be defined. If you intended to use a keyless entity type, call 'HasNoKey' in 'OnModelCreating'.
        // For more information on keyless entity types, see https://go.microsoft.com/fwlink/?linkid=2141943.' was thrown while
        // attempting to create an instance. For the different patterns supported at design time,
        // see https://go.microsoft.com/fwlink/?linkid=851728
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(FonbecWebDbContext).Assembly);
    }
}