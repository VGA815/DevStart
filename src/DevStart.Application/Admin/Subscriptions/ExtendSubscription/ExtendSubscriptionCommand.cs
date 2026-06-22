using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Admin.Subscriptions.ExtendSubscription
{
    public sealed record ExtendSubscriptionCommand(
        Guid SubscriptionId,
        int AdditionalDays,
        string Reason) : ICommand;
}
