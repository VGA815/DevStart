using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestmentApplications.GetById
{
    internal sealed class GetInvestmentApplicationByIdQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext)
        : IQueryHandler<GetInvestmentApplicationByIdQuery, InvestmentApplicationResponse>
    {
        public async Task<Result<InvestmentApplicationResponse>> Handle(GetInvestmentApplicationByIdQuery query, CancellationToken cancellationToken)
        {
            InvestmentApplication? application = await context.InvestmentApplications
                .AsNoTracking()
                .SingleOrDefaultAsync(a => a.Id == query.ApplicationId, cancellationToken);

            if (application is null)
            {
                return Result.Failure<InvestmentApplicationResponse>(
                    InvestmentApplicationErrors.NotFound(query.ApplicationId));
            }

            Guid userId = userContext.UserId;
            bool isInvestor = application.InvestorProfileId == userId;
            bool isFounderOrAdmin = false;

            if (!isInvestor)
            {
                isFounderOrAdmin = await context.StartupMembers
                    .AsNoTracking()
                    .AnyAsync(sm => sm.StartupId == application.StartupId
                                 && sm.ProfileId == userId
                                 && (sm.Role == StartupRole.Founder || sm.Role == StartupRole.Administration),
                              cancellationToken);
            }

            if (!isInvestor && !isFounderOrAdmin)
            {
                return Result.Failure<InvestmentApplicationResponse>(InvestmentApplicationErrors.Unauthorized);
            }

            return new InvestmentApplicationResponse
            {
                Id = application.Id,
                InvestorProfileId = application.InvestorProfileId,
                StartupId = application.StartupId,
                RoadmapItemId = application.RoadmapItemId,
                Amount = application.Amount,
                Message = application.Message,
                Status = application.Status,
                CreatedAt = application.CreatedAt,
                UpdatedAt = application.UpdatedAt
            };
        }
    }
}
