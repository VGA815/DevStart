using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.UserPreferences;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.UserPreferences.GetById
{
    internal sealed class GetUserPreferenceByIdQueryHandler(IApplicationDbContext context, IUserContext userContext)
        : IQueryHandler<GetUserPreferenceByIdQuery, UserPreferenceResponse>
    {
        public async Task<Result<UserPreferenceResponse>> Handle(GetUserPreferenceByIdQuery query, CancellationToken cancellationToken)
        {

            UserPreferenceResponse? userPreference = await context.Preferences
                .Where(up => up.UserId == query.UserId && up.UserId == userContext.UserId)
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
