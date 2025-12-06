using DevStart.Domain.Users;
using DevStart.SharedKernel;

namespace DevStart.Application.Users.Register
{
    internal sealed class UserRegisteredDomainEventHandler : IDomainEventHandler<UserRegisteredDomainEvent>
    {
        public Task Handle(UserRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
