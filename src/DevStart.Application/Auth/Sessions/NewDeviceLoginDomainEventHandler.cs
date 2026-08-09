using DevStart.Application.Abstractions.BackgroundJobs;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.RefreshTokens;
using DevStart.SharedKernel;

namespace DevStart.Application.Auth.Sessions
{
    /// <summary>
    /// Queues the "new device" warning email. Domain events are dispatched inside SaveChangesAsync, so
    /// anything slow here would be paid for by the login request — hence enqueue, never send.
    /// </summary>
    internal sealed class NewDeviceLoginDomainEventHandler(
        ICacheService cacheService,
        IBackgroundJobScheduler backgroundJobs) : IDomainEventHandler<NewDeviceLoginDomainEvent>
    {
        private static readonly TimeSpan DedupeWindow = TimeSpan.FromHours(24);

        public async Task Handle(NewDeviceLoginDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            // Guards against a burst of logins racing the database check in RefreshTokenService: the
            // first one wins and the rest are silent for a day.
            string key = $"login-alert:{domainEvent.UserId}:{domainEvent.Browser}:{domainEvent.Os}";

            if (await cacheService.GetAsync<bool?>(key, cancellationToken) is not null)
            {
                return;
            }

            await cacheService.SetAsync(key, true, DedupeWindow, cancellationToken);

            backgroundJobs.EnqueueNewDeviceLoginEmail(
                domainEvent.Email,
                domainEvent.Browser,
                domainEvent.Os,
                domainEvent.IpAddress,
                domainEvent.OccurredAtUtc);
        }
    }
}
