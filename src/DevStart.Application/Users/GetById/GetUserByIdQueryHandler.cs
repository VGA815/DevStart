using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Users;
using DevStart.SharedKernel;

namespace DevStart.Application.Users.GetById
{
    internal sealed class GetUserByIdQueryHandler(
        IQueryHandler<FetchUserByIdQuery, UserResponse> fetchHandler,
        IUserContext userContext)
        : IQueryHandler<GetUserByIdQuery, UserResponse>
    {
        public async Task<Result<UserResponse>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
        {
            // Own-account gate runs on every request, before the cached read is reached — so a warm
            // cache can never let one user read another user's record.
            if (query.UserId != userContext.UserId)
            {
                return Result.Failure<UserResponse>(UserErrors.Unauthorized());
            }

            return await fetchHandler.Handle(new FetchUserByIdQuery(query.UserId), cancellationToken);
        }
    }
}
