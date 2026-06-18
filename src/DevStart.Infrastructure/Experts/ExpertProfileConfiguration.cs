using DevStart.Domain.Experts;
using DevStart.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Experts
{
    internal sealed class ExpertProfileConfiguration : IEntityTypeConfiguration<ExpertProfile>
    {
        public void Configure(EntityTypeBuilder<ExpertProfile> builder)
        {
            builder.ToTable("expert_profiles");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("ix_expert_profiles_user_id");

            // Personal data lives on the shared Profile, referenced by UserId (1:1).
            builder.HasOne(x => x.Profile)
                .WithOne()
                .HasForeignKey<ExpertProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
