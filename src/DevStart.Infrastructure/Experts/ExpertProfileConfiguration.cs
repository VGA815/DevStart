using DevStart.Domain.Experts;
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
            builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired().HasColumnName("display_name");
            builder.Property(x => x.Bio).HasMaxLength(2000).HasColumnName("bio");
            builder.Property(x => x.Website).HasMaxLength(500).HasColumnName("website");
            builder.Property(x => x.IsPublic).HasColumnName("is_public");
            builder.Property(x => x.LinkedInUrl).HasMaxLength(500).HasColumnName("linkedin_url");
            builder.Property(x => x.TwitterUrl).HasMaxLength(500).HasColumnName("twitter_url");
            builder.Property(x => x.GitHubUrl).HasMaxLength(500).HasColumnName("github_url");
            builder.Property(x => x.TelegramUrl).HasMaxLength(500).HasColumnName("telegram_url");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("ix_expert_profiles_user_id");
        }
    }
}
