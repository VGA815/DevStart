using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;

namespace DevStart.Application.StartupProducts.Update
{
    internal sealed class UpdateStartupProductCommandHandler(IApplicationDbContext context, IUserContext userContext)
        : ICommandHandler<UpdateStartupProductCommand>
    {
        public Task<Result> Handle(UpdateStartupProductCommand command, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
