using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Investors;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestorProfiles.GetById
{
    internal sealed class GetInvestorProfileByIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetInvestorProfileByIdQuery, InvestorProfileResponse>
    {
        public async Task<Result<InvestorProfileResponse>> Handle(GetInvestorProfileByIdQuery query, CancellationToken cancellationToken)
        {
            InvestorProfileResponse? response = await context.InvestorProfiles
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
                    // Фонд показывается своим логотипом (без подстановки фото владельца — если логотипа
                    // нет, клиент рисует инициалы названия), физлицо — аватаркой основного аккаунта.
                    AvatarId = ip.Type == InvestorProfileType.Fund ? ip.AvatarId : ip.Profile.AvatarId,
                    FundAvatarId = ip.AvatarId,
                    CreatedAt = ip.CreatedAt,
                    UpdatedAt = ip.UpdatedAt
                })
                .SingleOrDefaultAsync(cancellationToken);

            // Непубличный профиль скрыт от всех, кроме владельца, и скрыт именно как «нет такого»:
            // каталог его не показывает, значит и по прямой ссылке существование подтверждать нечем.
            if (response is null || (!response.IsPublic && query.ViewerId != query.UserId))
            {
                return Result.Failure<InvestorProfileResponse>(InvestorProfileErrors.NotFound(query.UserId));
            }

            return response;
        }
    }
}
