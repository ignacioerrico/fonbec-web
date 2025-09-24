using Fonbec.Web.DataAccess.Configurations.Abstract;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class StudentConfiguration : UserWithoutAccountConfiguration<Student>
{
    public override void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.Property(s => s.Email)
            .IsRequired(false)
            .HasMaxLength(MaxLength.FonbecWebUser.Email);

        builder.Property(s => s.Notes)
            .IsRequired(false)
            .HasMaxLength(MaxLength.FonbecWebUser.Notes);

        builder.Property(s => s.SecondarySchoolStartYear)
            .IsRequired(false);

        builder.Property(s => s.UniversityStartYear)
            .IsRequired(false);

        builder.HasOne(s => s.Facilitator)
            .WithMany(p => p.Students)
            .HasForeignKey(s => s.FacilitatorId)
            .OnDelete(DeleteBehavior.NoAction);

        base.Configure(builder);
    }
}