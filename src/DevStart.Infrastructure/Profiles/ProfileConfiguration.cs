using DevStart.Domain.Profiles;
using DevStart.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Profiles
{
    internal sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
    {
        public void Configure(EntityTypeBuilder<Profile> builder)
        {
            builder.ToTable("profiles");

            builder.HasKey(x => x.UserId);

            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.Name).HasColumnName("name");
            builder.Property(x => x.Bio).HasColumnName("bio");
            builder.Property(x => x.Url).HasColumnName("url");
            builder.Property(x => x.IsAvailableForHire).HasColumnName("is_available_for_hire");
            builder.Property(x => x.IsPublic).HasColumnName("is_public");
            builder.Property(x => x.AvatarUrl).HasColumnName("avatar_url");

            builder.Property(x => x.SocialMediaLinks)
                .HasColumnName("social_media_links")
                .HasConversion(EfJson.StringListConverter)
                .HasColumnType("jsonb");
        }
    }
}
