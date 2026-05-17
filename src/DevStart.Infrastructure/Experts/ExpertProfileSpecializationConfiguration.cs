using DevStart.Domain.Experts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Experts
{
    internal sealed class ExpertProfileSpecializationConfiguration : IEntityTypeConfiguration<ExpertProfileSpecialization>
    {
        public void Configure(EntityTypeBuilder<ExpertProfileSpecialization> builder)
        {
            builder.ToTable("expert_profile_specializations");

            builder.HasKey(x => new { x.ExpertProfileId, x.Specialization });

            builder.Property(x => x.ExpertProfileId).HasColumnName("expert_profile_id");
            builder.Property(x => x.Specialization)
                .HasConversion<int>()
                .IsRequired()
                .HasColumnName("specialization");

            builder.HasIndex(x => x.Specialization).HasDatabaseName("ix_expert_profile_specializations_specialization");
        }
    }
}
