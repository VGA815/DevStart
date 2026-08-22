namespace DevStart.Domain.Common
{
    /// <summary>
    /// Reduces a website to the domain the platform dedups by. Shared, because more than one list on a
    /// startup is keyed this way — competitor cards and partnership records — and two copies of this
    /// rule would eventually disagree about "www." or a trailing dot, which is exactly the kind of
    /// drift that turns one dedup key into two.
    /// </summary>
    public static class WebsiteDomain
    {
        /// <summary>
        /// Host only, lower-cased, without a leading "www." or a trailing dot. Returns <c>null</c> for
        /// anything that is not an absolute http(s) URL — the validators reject those before they get
        /// here, so a null means "not comparable" rather than "duplicate of every other null".
        /// </summary>
        public static string? Normalize(string? website)
        {
            if (string.IsNullOrWhiteSpace(website)
                || !Uri.TryCreate(website.Trim(), UriKind.Absolute, out Uri? uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return null;
            }

            string host = uri.Host.ToLowerInvariant().TrimEnd('.');
            if (host.StartsWith("www.", StringComparison.Ordinal))
            {
                host = host[4..];
            }

            return host.Length == 0 ? null : host;
        }
    }
}
