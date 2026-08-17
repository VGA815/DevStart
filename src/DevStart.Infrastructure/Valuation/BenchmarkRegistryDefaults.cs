using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;

namespace DevStart.Infrastructure.Valuation
{
    /// <summary>
    /// The starting content of the two registry tables. Public so the coverage test can assert against
    /// it: every <see cref="Industry"/> value must either resolve to at least one Damodaran bucket here
    /// or be listed in <see cref="IndustriesWithoutDamodaranBucket"/>. Adding a value to the enum and
    /// nothing else fails that test on purpose — an unmapped sector is a sector whose Comparable method
    /// silently never fires.
    ///
    /// These are defaults, not doctrine. Both tables are editable through the admin API precisely so a
    /// new listing or a renamed Damodaran bucket never needs a release.
    /// </summary>
    public static class BenchmarkRegistryDefaults
    {
        /// <summary>
        /// Curated Russian public comparables. <c>Inn</c> is deliberately left <c>null</c> on every seeded
        /// row: an INN is what the ГИР БО job queries by, and seeding a guessed one would pull a
        /// different company's revenue into a multiple without anything looking wrong. An admin fills
        /// them in (or enters a consolidated <c>RevenueOverride</c> instead) before the revenue job has
        /// anything to do. Market-cap collection needs only the ticker and works from the first run.
        /// </summary>
        public static readonly IReadOnlyList<IssuerSeed> Issuers =
        [
            // SaaS / enterprise software — the deepest Russian public bench.
            new("POSI", Industry.Saas, "Positive Technologies", "Кибербезопасность, подписочная модель"),
            new("ASTR", Industry.Saas, "Группа Астра", "Инфраструктурное ПО, лицензии + подписка"),
            new("DIAS", Industry.Saas, "Диасофт", "ПО для финансового сектора"),
            new("IVAT", Industry.Saas, "ИВА Технолоджис", "Корпоративные коммуникации"),
            new("SOFL", Industry.Saas, "Софтлайн", "Смешанная модель: дистрибуция + собственные продукты — проверить сопоставимость"),

            // AI / internet platforms — no pure-play AI issuer exists, so these stand in for it.
            new("YDEX", Industry.Ai, "Яндекс", "Ближайший публичный прокси для AI/ML-продуктов"),
            new("VKCO", Industry.Ai, "VK", "Интернет-платформа с ML-ядром"),

            // Fintech.
            new("T", Industry.Fintech, "Т-Технологии", "Финтех-модель, не классический банк"),
            new("SVCB", Industry.Fintech, "Совкомбанк", "Банк с сильным цифровым сегментом — прокси"),

            // E-commerce.
            new("OZON", Industry.Ecommerce, "Озон", "Крупнейший публичный e-commerce РФ"),

            // Marketplaces / classifieds — take-rate платформы.
            new("HEAD", Industry.Marketplace, "ХэдХантер", "Классифайд с take-rate моделью"),
            new("CIAN", Industry.Marketplace, "ЦИАН", "Классифайд недвижимости"),

            // Biotech / pharma.
            new("PRMD", Industry.Biotech, "Промомед", "Фарма с собственной разработкой"),
            new("OZPH", Industry.Biotech, "Озон Фармацевтика", "Дженерики — ближе к фарме, чем к биотеху"),
            new("ABIO", Industry.Biotech, "Артген биотех", "Биотех"),
            new("GECO", Industry.Biotech, "Генетико", "Генетические исследования"),

            // Hardware / electronics.
            new("ELMT", Industry.Hardware, "Элемент", "Микроэлектроника"),

            // Telecom-adjacent broad-market anchors for the Other bucket.
            new("MTSS", Industry.Other, "МТС", "Широкий рынок, якорь для сборного сектора"),
            new("RTKM", Industry.Other, "Ростелеком", "Широкий рынок, якорь для сборного сектора"),
        ];

        /// <summary>
        /// Damodaran bucket → sector. Many buckets may point at one sector; a bucket never points at
        /// two, which is what the unique key on (source, external_key) enforces.
        ///
        /// <see cref="Industry.Other"/> is mapped on purpose. It is the enum's default value, so it is
        /// the sector of every startup that never picked one — and <c>RevenueMultiple</c> is looked up
        /// by exact sector, which means a row for <c>Other</c> is read by exactly that (large) group.
        /// It is a real sector here, not a fallback bucket.
        /// </summary>
        public static readonly IReadOnlyList<MappingSeed> DamodaranBuckets =
        [
            new("Software (System & Application)", Industry.Saas, null),
            new("Computer Services", Industry.Saas, null),
            new("Software (Internet)", Industry.Ai, "Ближайшая корзина: чистой AI-корзины у Damodaran нет"),
            new("Information Services", Industry.Ai, "Данные и аналитика как прокси AI"),
            new("Financial Svcs. (Non-bank & Insurance)", Industry.Fintech, "Небанковские финуслуги — ближе к финтеху, чем Banks"),
            new("Retail (Online)", Industry.Ecommerce, null),
            new("Business & Consumer Services", Industry.Marketplace, "Прокси для take-rate платформ: отдельной корзины маркетплейсов нет"),
            new("Advertising", Industry.Marketplace, "Второй прокси: классифайды монетизируются рекламно"),
            new("Electronics (General)", Industry.Hardware, null),
            new("Computers/Peripherals", Industry.Hardware, null),
            new("Drugs (Biotechnology)", Industry.Biotech, null),
            new("Drugs (Pharmaceutical)", Industry.Biotech, null),
            new("Education", Industry.Edtech, null),
            new("Total Market", Industry.Other, "Сборный сектор: стартапы, не выбравшие отрасль, читают именно эту строку"),
        ];

        /// <summary>
        /// Sectors deliberately left without a Damodaran bucket. Empty today — every sector has at least
        /// a proxy. A future sector with genuinely no comparable bucket goes here with the reason, and
        /// the coverage test then passes for it without pretending a mapping exists.
        /// </summary>
        public static readonly IReadOnlyDictionary<Industry, string> IndustriesWithoutDamodaranBucket =
            new Dictionary<Industry, string>();

        /// <summary>
        /// Builds the entity the seeder writes. Extracted so the "seeded issuers carry no INN" policy is
        /// assertable in a unit test rather than only readable in a comment — an INN is what the ГИР БО
        /// job queries by, and a guessed one would pull another company's revenue into a multiple with
        /// nothing looking wrong.
        /// </summary>
        public static BenchmarkIssuer ToIssuer(IssuerSeed seed, DateTime utcNow) =>
            BenchmarkIssuer.Create(seed.Ticker, inn: null, seed.DisplayName, seed.Industry, seed.Note, utcNow);

        public static BenchmarkIndustryMapping ToMapping(MappingSeed seed, DateTime utcNow) =>
            BenchmarkIndustryMapping.Create(
                BenchmarkMappingSourceKind.Damodaran, seed.ExternalKey, seed.Industry, seed.Note, utcNow);

        public sealed record IssuerSeed(string Ticker, Industry Industry, string DisplayName, string? Note);

        public sealed record MappingSeed(string ExternalKey, Industry? Industry, string? Note);
    }
}
