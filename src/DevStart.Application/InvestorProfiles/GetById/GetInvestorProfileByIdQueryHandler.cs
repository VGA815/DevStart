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
            InvestorProfile? investorProfile = await context.InvestorProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(ip => ip.UserId == query.UserId, cancellationToken);

            if (investorProfile is null)
            {
                return Result.Failure<InvestorProfileResponse>(InvestorProfileErrors.NotFound(query.UserId));
            }

            return new InvestorProfileResponse
            {
                Id = investorProfile.Id,
                UserId = investorProfile.UserId,
                Type = investorProfile.Type,
                DisplayName = investorProfile.DisplayName,
                Bio = investorProfile.Bio,
                Website = investorProfile.Website,
                IsPublic = investorProfile.IsPublic,
                CreatedAt = investorProfile.CreatedAt,
                UpdatedAt = investorProfile.UpdatedAt
            };
        }
    }
}
