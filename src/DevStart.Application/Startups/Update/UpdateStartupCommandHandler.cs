using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;

namespace DevStart.Application.Startups.Update
{
    internal sealed class UpdateStartupCommandHandler(IApplicationDbContext context, IUserContext userContext, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<UpdateStartupCommand>
    {
        public async Task<Result> Handle(UpdateStartupCommand command, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
