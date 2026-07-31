using DevStart.SharedKernel;

namespace DevStart.Domain.ServiceOrders
{
    public static class ServiceOrderErrors
    {
        public static Error NotFound(Guid serviceOrderId) => Error.NotFound(
            "ServiceOrders.NotFound",
            $"The service order with id = '{serviceOrderId}' was not found.");

        public static readonly Error NotPayable = Error.Conflict(
            "ServiceOrders.NotPayable",
            "Only a pending service order can be marked paid.");

        public static readonly Error NotFulfillable = Error.Conflict(
            "ServiceOrders.NotFulfillable",
            "Only a paid service order can be fulfilled.");

        public static Error UnknownServiceType(string serviceType) => Error.Problem(
            "ServiceOrders.UnknownServiceType",
            $"No catalog entry (price) is configured for service '{serviceType}'.");

        public static readonly Error TargetRequired = Error.Validation(
            "ServiceOrders.TargetRequired",
            "This service is bought for a specific startup or deal; targetId is required.");

        public static Error TargetNotFound(Guid targetId) => Error.NotFound(
            "ServiceOrders.TargetNotFound",
            $"The target with id = '{targetId}' was not found.");

        public static readonly Error TargetNotAllowed = Error.Forbidden(
            "ServiceOrders.TargetNotAllowed",
            "You are not allowed to buy this service for the requested target.");

        public static readonly Error AlreadyOwned = Error.Conflict(
            "ServiceOrders.AlreadyOwned",
            "You already have active access to this service for this target.");

        public static readonly Error NotCancellable = Error.Conflict(
            "ServiceOrders.NotCancellable",
            "A refunded service order cannot be cancelled.");
    }
}
