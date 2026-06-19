using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertProfiles.GetById;
using DevStart.Application.InvestorProfiles.GetById;
using DevStart.Application.Profiles.GetById;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.Experts;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Users.GetOverview
{
    internal sealed class FetchUserOverviewQueryHandler(IApplicationDbContext context)
        : IQueryHandler<FetchUserOverviewQuery, UserOverviewResponse>
    {
        public async Task<Result<UserOverviewResponse>> Handle(FetchUserOverviewQuery query, CancellationToken cancellationToken)
        {
            var user = await context.Users
                .AsNoTracking()
                .Where(u => u.Id == query.UserId)
                .Select(u => new { u.Id, u.Username, u.Email })
                .SingleOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                return Result.Failure<UserOverviewResponse>(UserErrors.NotFound(query.UserId));
            }

            ProfileResponse? profile = await context.Profiles
                .AsNoTracking()
                .Where(p => p.UserId == query.UserId)
                .Select(p => new ProfileResponse
                {
                    UserId = p.UserId,
                    AvatarId = p.AvatarId,
                    Bio = p.Bio,
                    IsAvailableForHire = p.IsAvailableForHire,
                    IsPublic = p.IsPublic,
                    Name = p.Name,
                    SocialMediaLinks = p.SocialMediaLinks,
                    Url = p.Url,
                    LinkedInUrl = p.LinkedInUrl,
                    TwitterUrl = p.TwitterUrl,
                    GitHubUrl = p.GitHubUrl,
                    TelegramUrl = p.TelegramUrl,
                    ViewCount = p.ViewCount,
                })
                .SingleOrDefaultAsync(cancellationToken);

            InvestorProfileResponse? investor = await context.InvestorProfiles
                .AsNoTracking()
                .Where(ip => ip.UserId == query.UserId)
                .Select(ip => new InvestorProfileResponse
                {
                    Id = ip.Id,
                    UserId = ip.UserId,
                    Type = ip.Type,
                    DisplayName = ip.Profile.Name ?? string.Empty,
                    Bio = ip.Profile.Bio,
                    Website = ip.Profile.Url,
                    IsPublic = ip.Profile.IsPublic,
                    CreatedAt = ip.CreatedAt,
                    UpdatedAt = ip.UpdatedAt
                })
                .SingleOrDefaultAsync(cancellationToken);

            (ExpertProfileResponse? expert, int acceptedCollaborationsCount, int experiencesCount) =
                await LoadExpertAsync(query.UserId, cancellationToken);

            (int completedDealsCount, decimal? totalInvestedAmount) =
                await LoadInvestorStatsAsync(investor?.Id, cancellationToken);

            var response = new UserOverviewResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Profile = profile,
                InvestorProfile = investor,
                ExpertProfile = expert,
                Statistics = new UserStatisticsResponse
                {
                    IsInvestor = investor is not null,
                    IsExpert = expert is not null,
                    CompletedDealsCount = completedDealsCount,
                    TotalInvestedAmount = totalInvestedAmount,
                    AcceptedCollaborationsCount = acceptedCollaborationsCount,
                    ExperiencesCount = experiencesCount,
                }
            };

            return response;
        }

        private async Task<(ExpertProfileResponse? Expert, int AcceptedCollaborationsCount, int ExperiencesCount)> LoadExpertAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var data = await context.ExpertProfiles
                .AsNoTracking()
                .Where(ep => ep.UserId == userId)
                .Select(ep => new
                {
                    ep.Id,
                    ep.UserId,
                    ep.CreatedAt,
                    ep.UpdatedAt,
                    ep.Profile.Name,
                    ep.Profile.Bio,
                    ep.Profile.Url,
                    ep.Profile.IsPublic,
                    ep.Profile.LinkedInUrl,
                    ep.Profile.TwitterUrl,
                    ep.Profile.GitHubUrl,
                    ep.Profile.TelegramUrl
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (data is null)
            {
                return (null, 0, 0);
            }

            List<ExpertSpecialization> specializations = await context.ExpertProfileSpecializations
                .AsNoTracking()
                .Where(s => s.ExpertProfileId == data.Id)
                .Select(s => s.Specialization)
                .ToListAsync(cancellationToken);

            int experiencesCount = await context.ExpertExperiences
                .AsNoTracking()
                .CountAsync(e => e.ExpertProfileId == data.Id, cancellationToken);

            int acceptedCollaborationsCount = await context.ExpertCollaborationRequests
                .AsNoTracking()
                .CountAsync(
                    r => r.ExpertProfileId == data.Id && r.Status == ExpertCollaborationRequestStatus.Accepted,
                    cancellationToken);

            var expert = new ExpertProfileResponse
            {
                Id = data.Id,
                UserId = data.UserId,
                DisplayName = data.Name ?? string.Empty,
                Bio = data.Bio,
                Website = data.Url,
                IsPublic = data.IsPublic,
                LinkedInUrl = data.LinkedInUrl,
                TwitterUrl = data.TwitterUrl,
                GitHubUrl = data.GitHubUrl,
                TelegramUrl = data.TelegramUrl,
                Specializations = specializations,
                ExperiencesCount = experiencesCount,
                CreatedAt = data.CreatedAt,
                UpdatedAt = data.UpdatedAt
            };

            return (expert, acceptedCollaborationsCount, experiencesCount);
        }

        private async Task<(int CompletedDealsCount, decimal? TotalInvestedAmount)> LoadInvestorStatsAsync(
            Guid? investorProfileId,
            CancellationToken cancellationToken)
        {
            if (investorProfileId is null)
            {
                return (0, null);
            }

            IQueryable<InvestmentDeal> completedDeals = context.InvestmentDeals
                .AsNoTracking()
                .Where(d => d.InvestorProfileId == investorProfileId && d.Status == InvestmentDealStatus.Completed);

            int completedDealsCount = await completedDeals.CountAsync(cancellationToken);
            decimal totalInvestedAmount = await completedDeals.SumAsync(d => d.Amount, cancellationToken);

            return (completedDealsCount, totalInvestedAmount);
        }
    }
}
