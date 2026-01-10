using DevStart.Domain.Startups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevStart.Infrastructure.Startups
{
    internal sealed class StartupConfiguration : IEntityTypeConfiguration<Startup>
    {
        public void Configure(EntityTypeBuilder<Startup> builder)
        {
            builder.HasKey(x => x.Id);
        }
    }
}
