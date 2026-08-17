using DevStart.Domain.Startups;
using DevStart.SharedKernel;

namespace DevStart.Domain.Valuation
{
    /// <summary>
    /// Maps an external taxonomy entry onto one of our <see cref="Startups.Industry"/> values. Mutable
    /// by design, for the same reason as <see cref="BenchmarkIssuer"/>: it is how a number is obtained,
    /// not the number.
    ///
    /// <see cref="Industry"/> being <c>null</c> is a real answer — "this bucket maps to nothing we
    /// model" — and is what keeps an unmapped Damodaran row from silently landing in a sector.
    /// </summary>
    public sealed class BenchmarkIndustryMapping : Entity
    {
        public Guid Id { get; set; }

        public BenchmarkMappingSourceKind SourceKind { get; set; }

        /// <summary>Damodaran bucket name or ОКВЭД code. Unique within a <see cref="SourceKind"/>.</summary>
        public string ExternalKey { get; set; } = null!;

        /// <summary>Target sector; <c>null</c> means "deliberately not mapped".</summary>
        public Industry? Industry { get; set; }

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public BenchmarkIndustryMapping() { }

        public static BenchmarkIndustryMapping Create(
            BenchmarkMappingSourceKind sourceKind,
            string externalKey,
            Industry? industry,
            string? note,
            DateTime utcNow)
            => new()
            {
                Id = Guid.NewGuid(),
                SourceKind = sourceKind,
                ExternalKey = externalKey,
                Industry = industry,
                Note = note,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            };

        public void Update(Industry? industry, string? note, DateTime utcNow)
        {
            Industry = industry;
            Note = note;
            UpdatedAt = utcNow;
        }
    }
}
