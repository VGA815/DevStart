using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Admin.Subscriptions.RevokeSubscription
{
    public sealed record RevokeSubscriptionCommand(Guid SubscriptionId, string Reason) : ICommand;
}
