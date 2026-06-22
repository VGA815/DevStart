using DevStart.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Users
{
    internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.Username).HasColumnName("username").IsRequired().HasMaxLength(100);
            builder.Property(x => x.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
            builder.Property(x => x.PasswordHash).HasColumnName("password_hash");
            builder.Property(x => x.IsVerified).HasColumnName("is_verified");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.Role).HasColumnName("role").HasDefaultValue(UserSystemRole.User);
            builder.Property(x => x.IsBanned).HasColumnName("is_banned").HasDefaultValue(false);
            builder.Property(x => x.BanReason).HasColumnName("ban_reason").HasColumnType("text");
            builder.Property(x => x.BannedAt).HasColumnName("banned_at");
            builder.Property(x => x.BanExpiresAt).HasColumnName("ban_expires_at");
            builder.Property(x => x.BannedByUserId).HasColumnName("banned_by_user_id");

            builder.HasIndex(x => x.Email).IsUnique();
            builder.HasIndex(x => x.Username).IsUnique();
            builder.HasIndex(x => new { x.IsBanned, x.BanExpiresAt })
                .HasDatabaseName("ix_users_banned");
        }
    }
}
