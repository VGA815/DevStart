using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;

namespace DevStart.Infrastructure.Database
{
    public sealed class ApplicationDbContextFactory
        : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Migrations.json", optional: false)
            .Build();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(config.GetConnectionString("Database"), o => o.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Default)).UseSnakeCaseNamingConvention()
                .Options;

            return new ApplicationDbContext(
                options,
                new NoOpDomainEventsDispatcher());
        }
    }
}
