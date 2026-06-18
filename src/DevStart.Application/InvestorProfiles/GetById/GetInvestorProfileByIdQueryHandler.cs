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
                    CreatedAt = ip.CreatedAt,
                    UpdatedAt = ip.UpdatedAt
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (response is null)
            {
                return Result.Failure<InvestorProfileResponse>(InvestorProfileErrors.NotFound(query.UserId));
            }

            return response;
        }
    }
}
