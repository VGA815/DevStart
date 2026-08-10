using DevStart.Domain.Investors;
using DevStart.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Investors
{
    internal sealed class InvestorProfileConfiguration : IEntityTypeConfiguration<InvestorProfile>
    {
        public void Configure(EntityTypeBuilder<InvestorProfile> builder)
        {
            builder.ToTable("investor_profiles");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.Type)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("type");
            builder.Property(x => x.AvatarId).HasColumnName("avatar_id");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("ix_investor_profiles_user_id");
            builder.HasIndex(x => x.Type).HasDatabaseName("ix_investor_profiles_type");

            // Personal data lives on the shared Profile, referenced by UserId (1:1).
            builder.HasOne(x => x.Profile)
                .WithOne()
                .HasForeignKey<InvestorProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
