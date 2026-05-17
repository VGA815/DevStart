using DevStart.Domain.Experts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Experts
{
    internal sealed class ExpertExperienceConfiguration : IEntityTypeConfiguration<ExpertExperience>
    {
        public void Configure(EntityTypeBuilder<ExpertExperience> builder)
        {
            builder.ToTable("expert_experiences");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.ExpertProfileId).HasColumnName("expert_profile_id");
            builder.Property(x => x.Company).HasMaxLength(200).IsRequired().HasColumnName("company");
            builder.Property(x => x.Position).HasMaxLength(200).IsRequired().HasColumnName("position");
            builder.Property(x => x.StartDate).HasColumnName("start_date");
            builder.Property(x => x.EndDate).HasColumnName("end_date");
            builder.Property(x => x.Description).HasMaxLength(1000).HasColumnName("description");
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.ExpertProfileId).HasDatabaseName("ix_expert_experiences_expert_profile_id");
        }
    }
}
