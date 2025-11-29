using System.Threading;
using System.Threading.Tasks;

namespace DevStart.SharedKernel
{
    public interface IDomainEventHandler<in T> where T : IDomainEvent
    {
        Task Handle(T domainEvent, CancellationToken cancellationToken);
    }
}
