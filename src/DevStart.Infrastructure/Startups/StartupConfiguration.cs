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
            builder.Property(x => x.IsStopped).HasColumnName("is_stopped");
            builder.Property(x => x.Stage).HasColumnName("stage");
            builder.Property(x => x.Location).HasColumnName("location");
            builder.Property(x => x.BillingEmail).HasColumnName("billing_email");
            builder.Property(x => x.AvatarUrl).HasColumnName("avatar_url");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.Property(x => x.SocialMediaLinks)
                .HasColumnName("social_media_links")
                .HasConversion(EfJson.StringListConverter)
                .HasColumnType("jsonb");
        }
    }
}
