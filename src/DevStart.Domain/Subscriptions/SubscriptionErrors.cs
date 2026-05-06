using DevStart.SharedKernel;

namespace DevStart.Domain.Subscriptions
{
    public static class SubscriptionErrors
    {
        public static Error NotFound(Guid subscriptionId) => Error.NotFound(
            "Subscriptions.NotFound",
            $"The subscription with id = '{subscriptionId}' was not found.");

        public static readonly Error NoSubscriptionForUser = Error.NotFound(
            "Subscriptions.NoSubscriptionForUser",
            "The user does not have a subscription record yet.");

        public static readonly Error AlreadyActive = Error.Conflict(
            "Subscriptions.AlreadyActive",
            "User already has an active Pro subscription. Wait until it expires before purchasing a new one.");

        public static readonly Error WrongStatusForActivation = Error.Problem(
            "Subscriptions.WrongStatusForActivation",
            "Only a Pending subscription can be activated.");

        public static readonly Error ProRequired = Error.Forbidden(
            "Subscriptions.ProRequired",
            "An active Pro subscription is required to access this feature.");

        public static Error ProRequiredForFeature(string feature) => Error.Forbidden(
            "Subscriptions.ProRequiredForFeature",
            $"An active Pro subscription is required to access feature '{feature}'.");
    }
}
