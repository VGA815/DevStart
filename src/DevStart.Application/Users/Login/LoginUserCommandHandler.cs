using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Users.Login
{
    public sealed class LoginUserCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ITokenProvider tokenProvider) : ICommandHandler<LoginUserCommand, string>
    {
        public async Task<Result<string>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
        {
            User? user = await context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Email == command.Email, cancellationToken);
            
            if (user is null)
            {
                return Result.Failure<string>(UserErrors.NotFoundByEmail);
            }

            bool verified = passwordHasher.Verify(command.Password, user.PasswordHash);

            if (!verified)
            {
                return Result.Failure<string>(UserErrors.NotFoundByEmail);
            }

            string token = tokenProvider.Create(user);

            return token;
        }
    }
}
