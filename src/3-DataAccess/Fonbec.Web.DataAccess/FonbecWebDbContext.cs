using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Abstract;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Fonbec.Web.DataAccess;

public sealed class FonbecWebDbContext : IdentityDbContext<FonbecWebUser, FonbecWebRole, int>
{
    public FonbecWebDbContext(DbContextOptions<FonbecWebDbContext> options) : base(options)
    {
        ChangeTracker.StateChanged += UpdateTimestamps;
        ChangeTracker.Tracked += UpdateTimestamps;
    }

    internal DbSet<Chapter> Chapters => Set<Chapter>();

    internal DbSet<Student> Students => Set<Student>();

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

    private static void UpdateTimestamps(object? sender, EntityEntryEventArgs e)
    {
        if (e.Entry.Entity is not Auditable auditable)
        {
            return;
        }

        switch (e.Entry.State)
        {
            case EntityState.Added:
                auditable.IsActive = true;
                auditable.CreatedOnUtc = DateTime.UtcNow;
                break;
            case EntityState.Modified:
                // An Auditable entity is considered active if it has not been disabled or if it has been reenabled.
                auditable.IsActive = auditable.DisabledById is null
                                     || (auditable.ReenabledById is not null && auditable.ReenabledOnUtc is not null);

                // If the entity is active, set the LastUpdatedOn timestamp.
                // If the entity is reenabled, set the ReenabledOn timestamp.
                // If the entity is disabled, set the DisabledOn timestamp.
                // If the entity is not active, make sure to check if it is being re-enabled first, because we know it is already disabled.
                // Once an Auditable entity is disabled, it cannot be "un-disabled".
                if (auditable.IsActive)
                {
                    auditable.LastUpdatedOnUtc = DateTime.UtcNow;
                }
                else if (auditable.ReenabledById is not null)
                {
                    auditable.ReenabledOnUtc = DateTime.UtcNow;
                }
                else if (auditable.DisabledById is not null)
                {
                    auditable.DisabledOnUtc = DateTime.UtcNow;
                }
                break;
        }
    }
}