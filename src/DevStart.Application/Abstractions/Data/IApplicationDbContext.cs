using DevStart.Domain.ConsentDocuments;
using DevStart.Domain.DealDocuments;
using DevStart.Domain.ExternalLogins;
using DevStart.Domain.RefreshTokens;
using DevStart.Domain.UserConsents;
using DevStart.Domain.EmailVerificationTokens;
using DevStart.Domain.Experts;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.Payments;
using DevStart.Domain.Subscriptions;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.Investors;
using DevStart.Domain.InviteTokens;
using DevStart.Domain.MediaFiles;
using DevStart.Domain.Messages;
using DevStart.Domain.Notifications;
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
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Abstractions.Data
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<UserPreference> Preferences { get; }
        DbSet<Startup> Startups { get; }
        DbSet<StartupRoadmapItem> StartupRoadmapItems { get; }
        DbSet<StartupProduct> StartupProducts { get; }
        DbSet<StartupMetric> StartupMetrics { get; }
        DbSet<StartupMember> StartupMembers { get; }
        DbSet<StartupInvestor> StartupInvestors { get; }
        DbSet<StartupFollower> StartupFollowers { get; }
        DbSet<StartupDocumentFile> StartupDocumentsFiles { get; }
        DbSet<Profile> Profiles { get; }
        DbSet<MediaFile> MediaFiles { get; }
        DbSet<EmailVerificationToken> EmailVerificationTokens { get; }
        DbSet<Notification> Notifications { get; }
        DbSet<InviteToken> InviteTokens { get; }
        DbSet<Message> Messages { get; }
        DbSet<StartupDocumentFile> StartupDocumentFiles { get; }
        DbSet<InvestorProfile> InvestorProfiles { get; }
        DbSet<ExpertProfile> ExpertProfiles { get; }
        DbSet<ExpertProfileSpecialization> ExpertProfileSpecializations { get; }
        DbSet<ExpertExperience> ExpertExperiences { get; }
        DbSet<InvestmentApplication> InvestmentApplications { get; }
        DbSet<InvestmentDeal> InvestmentDeals { get; }
        DbSet<StartupCompetitor> StartupCompetitors { get; }
        DbSet<DealDocument> DealDocuments { get; }
        DbSet<Subscription> Subscriptions { get; }
        DbSet<Payment> Payments { get; }
        DbSet<UserConsent> UserConsents { get; }
        DbSet<ConsentDocument> ConsentDocuments { get; }
        DbSet<ExternalLogin> ExternalLogins { get; }
        DbSet<RefreshToken> RefreshTokens { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
