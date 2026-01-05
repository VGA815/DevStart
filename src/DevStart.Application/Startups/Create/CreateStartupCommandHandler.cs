using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupProducts;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Startups.Create
{
    internal sealed class CreateStartupCommandHandler(IApplicationDbContext context, IUserContext userContext, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CreateStartupCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateStartupCommand command, CancellationToken cancellationToken)
        {
            if (await context.Startups.AnyAsync(s => s.Name == command.Name, cancellationToken))
            {
                return Result.Failure<Guid>(StartupErrors.NameNotUnique);
            }
            if (command.UserId != userContext.UserId)
            {
                return Result.Failure<Guid>(UserErrors.Unauthorized());
            }
            if (!await context.Users.AnyAsync(u => u.Id == command.UserId))
            {
                return Result.Failure<Guid>(UserErrors.NotFound(command.UserId));
            }

            // TODO: Email addresses verification

            Startup startup = new Startup(
                command.Name,
                command.PublicEmail,
                command.Description,
                command.Url,
                command.IsStopped,
                command.Stage,
                command.SocialMediaLinks,
                command.Location,
                command.BillingEmail,
                command.AvatarUrl,
                dateTimeProvider);

            StartupMember startupMember = new StartupMember(
                command.UserId,
                startup.Id,
                StartupRole.Founder,
                true,
                dateTimeProvider);

            StartupProduct startupProduct = new StartupProduct(
                startup.Id,
                command.ProductName,
                command.ProductProblemSolution,
                command.Stack,
                command.ProductValueProposition,
                command.ProductDifferentiators);

            context.Startups.Add(startup);
            context.StartupMembers.Add(startupMember);
            context.StartupProducts.Add(startupProduct);

            await context.SaveChangesAsync(cancellationToken);

            return startup.Id;
        }
    }
}
