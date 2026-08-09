namespace DevStart.Application.Abstractions.Pagination
{
    /// <summary>
    /// Shared bounds for paged list queries. Page parameters arrive straight off the query string,
    /// so every listing has to narrow them itself before they reach Skip/Take.
    /// </summary>
    public static class Paging
    {
        /// <summary>
        /// Ceiling for a single page. Matches the limit the message listings already enforce
        /// (<c>Messages/GetConversation</c>). Admin listings keep their own, wider limit.
        /// </summary>
        public const int MaxPageSize = 100;

        /// <summary>Used when the caller sends a non-positive page size.</summary>
        public const int DefaultPageSize = 20;

        /// <summary>
        /// Clamps page parameters to a safe range: a negative page number otherwise makes Skip throw,
        /// and an unclamped page size returns the whole table in one request.
        /// </summary>
        public static (int PageNumber, int PageSize) Normalize(int pageNumber, int pageSize) =>
            (Math.Max(pageNumber, 1),
             pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize));
    }
}
