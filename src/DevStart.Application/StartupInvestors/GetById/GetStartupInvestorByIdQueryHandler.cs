using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupInvestors;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupInvestors.GetById
{
    internal sealed class GetStartupInvestorByIdQueryHandler(IApplicationDbContext context, IUserContext userContext)
        : IQueryHandler<GetStartupInvestorByIdQuery, StartupInvestorResponse>
    {
        public async Task<Result<StartupInvestorResponse>> Handle(GetStartupInvestorByIdQuery query, CancellationToken cancellationToken)
        {
            StartupInvestor? startupInvestor = await context.StartupInvestors
                .SingleOrDefaultAsync(si => si.StartupId == query.StartupId && si.ProfileId == query.ProfileId, cancellationToken);

            if (startupInvestor == null)
            {
                return Result.Failure<StartupInvestorResponse>(StartupInvestorErrors.NotFound(query.ProfileId, query.StartupId));
            }
            if (!startupInvestor.IsPublic && userContext.UserId != query.ProfileId)
            {
                return Result.Failure<StartupInvestorResponse>(UserErrors.Unauthorized());
            }

            StartupInvestorResponse response = new StartupInvestorResponse()
            {
                ProfileId = startupInvestor.ProfileId,
                CreatedAt = startupInvestor.CreatedAt,
                IsPublic = startupInvestor.IsPublic,
                StartupId = startupInvestor.StartupId,
                UpdatedAt = startupInvestor.UpdatedAt,
            };

            return response;
        }
    }
}
