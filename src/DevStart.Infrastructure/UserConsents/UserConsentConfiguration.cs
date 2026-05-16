using DevStart.Domain.UserConsents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.UserConsents
{
    internal sealed class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
    {
        public void Configure(EntityTypeBuilder<UserConsent> builder)
        {
            builder.ToTable("user_consents");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.Type)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("consent_type");
            builder.Property(x => x.DocumentVersion)
                .IsRequired()
                .HasMaxLength(10)
                .HasColumnName("document_version");
            builder.Property(x => x.AcceptedAt).HasColumnName("accepted_at");
            builder.Property(x => x.RevokedAt).HasColumnName("revoked_at");

            builder.Ignore(x => x.IsActive);

            builder.HasIndex(x => new { x.UserId, x.Type })
                .HasDatabaseName("ix_user_consents_user_id_consent_type");
        }
    }
}
