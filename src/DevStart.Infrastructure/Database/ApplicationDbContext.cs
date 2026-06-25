using DevStart.Application.Abstractions.Data;
using DevStart.Domain.Admin;
using DevStart.Domain.ConsentDocuments;
using DevStart.Domain.DealDocuments;
using DevStart.Domain.ExternalLogins;
using DevStart.Domain.PromoCodes;
using DevStart.Domain.RefreshTokens;
using DevStart.Domain.UserConsents;
using DevStart.Domain.EmailVerificationTokens;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.Experts;
using DevStart.Domain.Payments;
using DevStart.Domain.Subscriptions;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.Investors;
using DevStart.Domain.InviteTokens;
using DevStart.Domain.MediaFiles;
using DevStart.Domain.Messages;
using DevStart.Domain.Notifications;
using DevStart.Domain.PasswordResetTokens;
using DevStart.Domain.Profiles;
using DevStart.Domain.StartupCompetitors;
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

        public DbSet<StartupValuationSnapshot> StartupValuationSnapshots { get; set; }

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

        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<InviteToken> InviteTokens { get; set; }

        public DbSet<Message> Messages { get; set; }

        public DbSet<StartupDocumentFile> StartupDocumentFiles { get; set; }

        public DbSet<InvestorProfile> InvestorProfiles { get; set; }

        public DbSet<ExpertProfile> ExpertProfiles { get; set; }

        public DbSet<ExpertProfileSpecialization> ExpertProfileSpecializations { get; set; }

        public DbSet<ExpertExperience> ExpertExperiences { get; set; }

        public DbSet<ExpertCollaborationRequest> ExpertCollaborationRequests { get; set; }

        public DbSet<InvestmentApplication> InvestmentApplications { get; set; }

        public DbSet<InvestmentDeal> InvestmentDeals { get; set; }

        public DbSet<StartupCompetitor> StartupCompetitors { get; set; }

        public DbSet<DealDocument> DealDocuments { get; set; }

        public DbSet<Subscription> Subscriptions { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<UserConsent> UserConsents { get; set; }

        public DbSet<ConsentDocument> ConsentDocuments { get; set; }

        public DbSet<ExternalLogin> ExternalLogins { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<AdminActionLog> AdminActionLogs { get; set; }

        public DbSet<PromoCode> PromoCodes { get; set; }

        public DbSet<PromoCodeRedemption> PromoCodeRedemptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            modelBuilder.HasDefaultSchema(Schemas.Default);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
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
