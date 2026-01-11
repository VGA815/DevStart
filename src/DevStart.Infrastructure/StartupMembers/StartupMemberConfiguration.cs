using DevStart.Domain.StartupMembers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.StartupMembers
{
    internal sealed class StartupMemberConfiguration : IEntityTypeConfiguration<StartupMember>
    {
        public void Configure(EntityTypeBuilder<StartupMember> builder)
        {
            builder.ToTable("startup_members");

            builder.HasKey(x => new { x.ProfileId, x.StartupId });

            builder.Property(x => x.ProfileId).HasColumnName("profile_id");
            builder.Property(x => x.StartupId).HasColumnName("startup_id");
            builder.Property(x => x.Role).HasColumnName("role");
            builder.Property(x => x.IsPublic).HasColumnName("is_public");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        }
    }
}
