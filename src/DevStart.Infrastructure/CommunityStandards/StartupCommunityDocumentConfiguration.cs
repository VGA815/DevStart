using DevStart.Domain.StartupCommunityStandards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.CommunityStandards
{
    internal sealed class StartupCommunityDocumentConfiguration : IEntityTypeConfiguration<StartupCommunityDocument>
    {
        public void Configure(EntityTypeBuilder<StartupCommunityDocument> builder)
        {
            builder.ToTable("startup_community_documents");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.StartupId).HasColumnName("startup_id");

            builder.Property(x => x.Type)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("type");

            builder.Property(x => x.Title)
                .HasColumnName("title")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Content)
                .HasColumnName("content")
                .HasColumnType("text")
                .IsRequired();

            builder.Property(x => x.AuthorId).HasColumnName("author_id");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            // One document per type per startup — the upsert command relies on this being enforced.
            builder.HasIndex(x => new { x.StartupId, x.Type })
                .IsUnique()
                .HasDatabaseName("ix_startup_community_documents_startup_id_type");
        }
    }
}
