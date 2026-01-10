using DevStart.Application.Abstractions.Data;
using DevStart.Domain.EmailVerificationTokens;
using DevStart.Domain.MediaFiles;
using DevStart.Domain.Profiles;
using DevStart.Domain.StartupDocumentFiles;
using DevStart.Domain.StartupFollowers;
using DevStart.Domain.StartupInvestors;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupMetrics;
using DevStart.Domain.StartupProducts;
using DevStart.Domain.StartupRoadmapItems;
using DevStart.Domain.Startups;
using DevStart.Domain.UserPreferences;
using DevStart.Domain.Users;
using DevStart.Infrastructure.DomainEvents;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Infrastructure.Database
{
    public sealed class ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IDomainEventsDispatcher domainEventsDispatcher)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<User> Users { get; set; }

        public DbSet<UserPreference> Preferences { get; set; }

        public DbSet<Startup> Startups { get; set; }

        public DbSet<StartupRoadmapItem> StartupRoadmapItems { get; set; }

        public DbSet<StartupProduct> StartupProducts { get; set; }

        public DbSet<StartupMetric> StartupMetrics { get; set; }

        public DbSet<StartupMember> StartupMembers { get; set; }

        public DbSet<StartupInvestor> StartupInvestors { get; set; }

        public DbSet<StartupFollower> StartupFollowers { get; set; }

        public DbSet<StartupDocumentFile> StartupDocumentsFiles { get; set; }

        public DbSet<Profile> Profiles { get; set; }

        public DbSet<MediaFile> MediaFiles { get; set; }

        public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            modelBuilder.HasDefaultSchema(Schemas.Default);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            int result = await base.SaveChangesAsync(cancellationToken);

            await PublishDomainEventsAsync();

            return result;
        }
        private async Task PublishDomainEventsAsync()
        {
            var domainEvents = ChangeTracker
                .Entries<Entity>()
                .Select(e => e.Entity)
                .SelectMany(entity =>
                {
                    List<IDomainEvent> domainEvents = entity.DomainEvents;

                    entity.ClearDomainEvents();

                    return domainEvents;
                })
                .ToList();

            await domainEventsDispatcher.DispatchAsync(domainEvents);
        }
    }
}
