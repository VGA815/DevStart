using DevStart.Domain.Startups;
using DevStart.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Startups
{
    internal sealed class StartupConfiguration : IEntityTypeConfiguration<Startup>
    {
        public void Configure(EntityTypeBuilder<Startup> builder)
        {
            builder.ToTable("startups");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.Name).HasColumnName("name").IsRequired();
            builder.Property(x => x.PublicEmail).HasColumnName("public_email");
            builder.Property(x => x.Description).HasColumnName("description");
            builder.Property(x => x.Url).HasColumnName("url");
            builder.Property(x => x.ShortDescription).HasColumnName("short_description");
            builder.Property(x => x.IsStopped).HasColumnName("is_stopped");
            builder.Property(x => x.Stage).HasColumnName("stage");
            builder.Property(x => x.Location).HasColumnName("location");
            builder.Property(x => x.BillingEmail).HasColumnName("billing_email");
            builder.Property(x => x.AvatarId).HasColumnName("avatar_id");
            builder.Property(x => x.Tam).HasColumnName("tam").HasColumnType("numeric(18,2)");
            builder.Property(x => x.Sam).HasColumnName("sam").HasColumnType("numeric(18,2)");
            builder.Property(x => x.Som).HasColumnName("som").HasColumnType("numeric(18,2)");
            builder.Property(x => x.MarketGrowthRate).HasColumnName("market_growth_rate").HasColumnType("numeric(5,2)");
            builder.Property(x => x.HasPatents).HasColumnName("has_patents").HasDefaultValue(false);
            builder.Property(x => x.IsBanned).HasColumnName("is_banned").HasDefaultValue(false);
            builder.Property(x => x.BanReason).HasColumnName("ban_reason").HasColumnType("text");
            builder.Property(x => x.BannedAt).HasColumnName("banned_at");
            builder.Property(x => x.BanExpiresAt).HasColumnName("ban_expires_at");
            builder.Property(x => x.BannedByUserId).HasColumnName("banned_by_user_id");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => new { x.IsBanned, x.BanExpiresAt })
                .HasDatabaseName("ix_startups_banned");

            builder.Property(x => x.SocialMediaLinks)
                .HasColumnName("social_media_links")
                .HasConversion(EfJson.StringListConverter)
                .HasColumnType("jsonb");
        }
    }
}
