using DevStart.SharedKernel;

namespace DevStart.Domain.StartupProducts
{
    public static class StartupProductErrors
    {
        public static Error NotFound(Guid startupId) => Error.NotFound(
            "StartupProducts.NotFound",
            $"The startup product with startupId = '{startupId}' was not found");
        public static readonly Error IsNotUnique = Error.Conflict(
            "StartupProducts.IsNotUnique",
            $"The startup product with specified startupId is already exists");
    }
}
