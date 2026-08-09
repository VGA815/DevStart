namespace DevStart.Application.ExpertCollaborationRequests
{
    /// <summary>
    /// Clamps the paging inputs for both request lists. Same bounds as the admin lists (max 200) so a
    /// caller cannot ask for an unbounded page.
    /// </summary>
    internal static class CollaborationRequestPaging
    {
        public const int DefaultPageSize = 50;
        public const int MaxPageSize = 200;

        public static int Size(int requested) => requested is > 0 and <= MaxPageSize ? requested : DefaultPageSize;

        public static int Skip(int requestedPageNumber, int pageSize)
            => ((requestedPageNumber > 0 ? requestedPageNumber : 1) - 1) * pageSize;
    }
}
