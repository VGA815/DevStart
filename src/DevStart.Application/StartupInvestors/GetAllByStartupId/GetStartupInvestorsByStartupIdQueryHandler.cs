using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Investors;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupInvestors.GetAllByStartupId
{
    internal sealed class GetStartupInvestorsByStartupIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetStartupInvestorsByStartupIdQuery, List<StartupInvestorResponse>>
    {
        public async Task<Result<List<StartupInvestorResponse>>> Handle(GetStartupInvestorsByStartupIdQuery query, CancellationToken cancellationToken)
        {
            if (!await context.Startups.AnyAsync(s => s.Id == query.StartupId, cancellationToken))
            {
                return Result.Failure<List<StartupInvestorResponse>>(StartupErrors.NotFound(query.StartupId));
            }

            // Аватарка инвестора зависит от типа его профиля: фонд представлен собственным логотипом,
            // физлицо — аватаркой основного аккаунта. Оба варианта тянем одним запросом (левые
            // соединения), чтобы клиенту не пришлось добирать профили по одному.
            var rows = await (
                from si in context.StartupInvestors
                where si.IsPublic && si.StartupId == query.StartupId
                join ipJoin in context.InvestorProfiles on si.ProfileId equals ipJoin.UserId into ips
                from ip in ips.DefaultIfEmpty()
                join pJoin in context.Profiles on si.ProfileId equals pJoin.UserId into ps
                from p in ps.DefaultIfEmpty()
                select new
                {
                    si.StartupId,
                    si.IsPublic,
                    si.CreatedAt,
                    si.ProfileId,
                    si.UpdatedAt,
                    InvestorType = (InvestorProfileType?)ip.Type,
                    FundAvatarId = ip.AvatarId,
                    PersonalAvatarId = p.AvatarId
                })
                .ToListAsync(cancellationToken);

            List<StartupInvestorResponse> startupInvestorResponses = rows
                .Select(r => new StartupInvestorResponse
                {
                    StartupId = r.StartupId,
                    IsPublic = r.IsPublic,
                    CreatedAt = r.CreatedAt,
                    ProfileId = r.ProfileId,
                    UpdatedAt = r.UpdatedAt,
                    // У фонда без логотипа личное фото не подставляем — клиент нарисует инициалы.
                    AvatarId = r.InvestorType == InvestorProfileType.Fund
                        ? r.FundAvatarId
                        : r.PersonalAvatarId,
                    IsFund = r.InvestorType == InvestorProfileType.Fund,
                })
                .ToList();

            return startupInvestorResponses;
        }
    }
}
