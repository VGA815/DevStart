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
    }
}
