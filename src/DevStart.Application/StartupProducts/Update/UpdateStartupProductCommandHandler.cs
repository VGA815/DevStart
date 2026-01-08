using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupProducts;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupProducts.Update
{
    internal sealed class UpdateStartupProductCommandHandler(IApplicationDbContext context, IUserContext userContext)
        : ICommandHandler<UpdateStartupProductCommand>
    {
        public async Task<Result> Handle(UpdateStartupProductCommand command, CancellationToken cancellationToken)
        {
            StartupMember? startupMember = await context.StartupMembers
                .SingleOrDefaultAsync(sm => sm.StartupId == command.StartupId && sm.ProfileId == userContext.UserId, cancellationToken);

            if (startupMember == null)
            {
                return Result.Failure(StartupMemberErrors.NotFound(userContext.UserId, command.StartupId));
            }

            if (startupMember.Role == StartupRole.Member)
            {
                return Result.Failure(UserErrors.Unauthorized());
            }

            StartupProduct? startupProduct = await context.StartupProducts
                .SingleOrDefaultAsync(sp => sp.StartupId == command.StartupId, cancellationToken);

            startupProduct!.Solution = command.Solution;
            startupProduct.ValueProposition = command.ValueProposition;
            startupProduct.Differentiators = command.Differentiators;
            startupProduct.Stack = command.Stack;
            startupProduct.Problem = command.Problem;
            
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
