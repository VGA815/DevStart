using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.UserPreferences;
using DevStart.SharedKernel;

namespace DevStart.Application.UserPreferences.GetById
{
    internal sealed class GetUserPreferenceByIdQueryHandler(
        IQueryHandler<FetchUserPreferenceByIdQuery, UserPreferenceResponse> fetchHandler,
        IUserContext userContext)
        : IQueryHandler<GetUserPreferenceByIdQuery, UserPreferenceResponse>
    {
        public async Task<Result<UserPreferenceResponse>> Handle(GetUserPreferenceByIdQuery query, CancellationToken cancellationToken)
        {
            // Own-account gate runs on every request, before the cached read is reached — so a warm
            // cache can never let one user read another user's preferences. Cross-account requests
            // get NotFound (rather than a distinct "forbidden"), preserving enumeration-safe behavior.
            if (query.UserId != userContext.UserId)
            {
                return Result.Failure<UserPreferenceResponse>(UserPreferenceErrors.NotFound(query.UserId));
            }

            return await fetchHandler.Handle(new FetchUserPreferenceByIdQuery(query.UserId), cancellationToken);
        }
    }
}
