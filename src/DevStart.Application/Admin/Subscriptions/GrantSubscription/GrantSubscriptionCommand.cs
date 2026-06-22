using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Admin.Subscriptions.GrantSubscription
{
    /// <summary>Grants a complimentary Pro subscription (no payment). Null duration uses the plan default.</summary>
    public sealed record GrantSubscriptionCommand(
        Guid UserId,
        int? DurationDays,
        string Reason) : ICommand<Guid>;
}
