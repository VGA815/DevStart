using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Users.Register
{
    internal sealed class RegisterUserCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
        : ICommandHandler<RegisterUserCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
        {
            if (await context.Users.AnyAsync(u => u.Email == command.Email, cancellationToken))
            {
                return Result.Failure<Guid>(UserErrors.EmailNotUnique);
            }

            User user = new User()
            {
                Id = Guid.NewGuid(),
                Email = command.Email,
                PasswordHash = passwordHasher.Hash(command.Password),
                Username = command.Username
            };

            user.Raise(new UserRegisteredDomainEvent(user.Id));

            context.Users.Add(user);

            await context.SaveChangesAsync(cancellationToken);

            return user.Id;
        }
    }
}
