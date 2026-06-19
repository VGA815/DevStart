using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.UserPreferences;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.UserPreferences.GetById
{
    internal sealed class FetchUserPreferenceByIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<FetchUserPreferenceByIdQuery, UserPreferenceResponse>
    {
        public async Task<Result<UserPreferenceResponse>> Handle(FetchUserPreferenceByIdQuery query, CancellationToken cancellationToken)
        {
            UserPreferenceResponse? userPreference = await context.Preferences
                .Where(up => up.UserId == query.UserId)
                .Select(up => new UserPreferenceResponse
                {
                    UserId = up.UserId,
                    ReceiveNotifications = up.ReceiveNotifications,
                    Theme = up.Theme,
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (userPreference is null)
            {
                return Result.Failure<UserPreferenceResponse>(UserPreferenceErrors.NotFound(query.UserId));
            }

            return userPreference;
        }
    }
}
