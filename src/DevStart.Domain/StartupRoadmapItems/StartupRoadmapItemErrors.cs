using DevStart.SharedKernel;

namespace DevStart.Domain.StartupRoadmapItems
{
    public static class StartupRoadmapItemErrors
    {
        public static Error NotFound(Guid itemId) => Error.NotFound(
            "StartupRoadmapItems.NotFound",
            $"The startup roadmap item with Id = '{itemId}' was not found");
        public static readonly Error NotFoundByStartupId = Error.NotFound(
            "StartupRoadmapItems.NotFoundByStartupId",
            "The startup roadmap item with specified startupId was not found");
    }
}
