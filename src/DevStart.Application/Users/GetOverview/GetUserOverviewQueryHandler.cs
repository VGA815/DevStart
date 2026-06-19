using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;

namespace DevStart.Application.Users.GetOverview
{
    internal sealed class GetUserOverviewQueryHandler(
        IQueryHandler<FetchUserOverviewQuery, UserOverviewResponse> fetchHandler,
        IUserContext userContext)
        : IQueryHandler<GetUserOverviewQuery, UserOverviewResponse>
    {
        public async Task<Result<UserOverviewResponse>> Handle(GetUserOverviewQuery query, CancellationToken cancellationToken)
        {
            Result<UserOverviewResponse> result =
                await fetchHandler.Handle(new FetchUserOverviewQuery(query.UserId), cancellationToken);

            if (result.IsFailure)
            {
                return result;
            }

            UserOverviewResponse overview = result.Value;

            // Owner sees everything. The cached fetch is viewer-independent and holds the full
            // aggregate (including private fields), so the redaction below runs AFTER the cache —
            // a warm cache can never disclose another user's Email or investment volume.
            if (query.UserId == userContext.UserId)
            {
                return overview;
            }

            return overview with
            {
                Email = null,
                Statistics = overview.Statistics with { TotalInvestedAmount = null }
            };
        }
    }
}
