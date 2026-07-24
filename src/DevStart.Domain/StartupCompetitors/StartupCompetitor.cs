using DevStart.SharedKernel;

namespace DevStart.Domain.StartupCompetitors
{
    public sealed class StartupCompetitor : Entity
    {
        /// <summary>
        /// Upper bound on competitor cards per startup. The scoring factor saturates well below this,
        /// so the limit exists purely to stop the list being padded with placeholders.
        /// </summary>
        public const int MaxPerStartup = 50;

        public Guid Id { get; set; }
        public Guid StartupId { get; set; }
        public string Name { get; set; } = null!;

        /// <summary>
        /// Mandatory on every write (<see cref="Create"/>/<see cref="Update"/> take it non-null), but
        /// nullable in storage: rows created before it became mandatory carry no value and are left
        /// untouched — they acquire one on their next update.
        /// </summary>
        public string? Website { get; set; }

        /// <summary>
        /// Host of <see cref="Website"/>, lower-cased and stripped of a leading "www." — the dedup key
        /// within a startup. <c>null</c> for the same legacy rows, which is why the unique index keeps
        /// nulls distinct.
        /// </summary>
        public string? NormalizedDomain { get; set; }

        public string? Description { get; set; }
        public string? StrengthsVsUs { get; set; }
        public string? WeaknessesVsUs { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public StartupCompetitor() { }

        public static StartupCompetitor Create(
            Guid startupId,
            string name,
            string website,
            string? description,
            string? strengthsVsUs,
            string? weaknessesVsUs,
            DateTime createdAt)
            => new()
            {
                Id = Guid.NewGuid(),
                StartupId = startupId,
                Name = name,
                Website = website,
                NormalizedDomain = NormalizeDomain(website),
                Description = description,
                StrengthsVsUs = strengthsVsUs,
                WeaknessesVsUs = weaknessesVsUs,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

        public void Update(
            string name,
            string website,
            string? description,
            string? strengthsVsUs,
            string? weaknessesVsUs,
            DateTime utcNow)
        {
            Name = name;
            Website = website;
            NormalizedDomain = NormalizeDomain(website);
            Description = description;
            StrengthsVsUs = strengthsVsUs;
            WeaknessesVsUs = weaknessesVsUs;
            UpdatedAt = utcNow;
        }

        /// <summary>
        /// Reduces a website to its comparable domain: host only, lower-cased, without a leading
        /// "www." or a trailing dot. Returns <c>null</c> for anything that is not an absolute http(s)
        /// URL — the validators reject those before they reach here, so a null means "not comparable"
        /// rather than "duplicate of every other null".
        /// </summary>
        public static string? NormalizeDomain(string? website)
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
