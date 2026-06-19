using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Users.GetById
{
    internal sealed class FetchUserByIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<FetchUserByIdQuery, UserResponse>
    {
        public async Task<Result<UserResponse>> Handle(FetchUserByIdQuery query, CancellationToken cancellationToken)
        {
            UserResponse? user = await context.Users
                .Where(u => u.Id == query.UserId)
                .Select(u => new UserResponse
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                return Result.Failure<UserResponse>(UserErrors.NotFound(query.UserId));
            }

            return user;
        }
    }
}
