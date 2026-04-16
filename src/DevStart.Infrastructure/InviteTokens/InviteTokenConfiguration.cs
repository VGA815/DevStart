using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DevStart.Domain.InviteTokens;

namespace DevStart.Infrastructure.InviteTokens
{
    internal sealed class InviteTokenConfiguration : IEntityTypeConfiguration<InviteToken>
    {
        public void Configure(EntityTypeBuilder<InviteToken> builder)
        {
            builder.ToTable("invite_tokens");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id).HasColumnName("id");
            builder.Property(t => t.StartupId).HasColumnName("startup_id").IsRequired();
            builder.Property(t => t.ExpiresAt).HasColumnName("expires_at");
            builder.Property(t => t.IsUsed).HasColumnName("is_used");

            builder.HasIndex(t => t.StartupId);
            builder.HasIndex(t => new { t.IsUsed, t.ExpiresAt });
        }
    }
}
