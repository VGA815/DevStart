using DevStart.Application.CommunityStandards;
using DevStart.Domain.StartupCommunityStandards;
using System.Reflection;

namespace DevStart.Infrastructure.CommunityStandards
{
    /// <summary>
    /// Serves the starter texts from embedded Markdown resources, the same delivery mechanism as the
    /// consent documents and term-sheet templates. Read once on first use and kept in memory — the
    /// texts ship with the assembly and cannot change at runtime.
    /// </summary>
    internal sealed class CommunityDocumentTemplateProvider : ICommunityDocumentTemplateProvider
    {
        private static readonly (string ResourceSuffix, CommunityDocumentType Type, string Title)[] Definitions =
        [
            ("CommunityStandards.Templates.code-of-conduct.md",  CommunityDocumentType.CodeOfConduct,  "Кодекс поведения"),
            ("CommunityStandards.Templates.contributing.md",     CommunityDocumentType.Contributing,   "Как присоединиться и участвовать"),
            ("CommunityStandards.Templates.support.md",          CommunityDocumentType.Support,        "Поддержка"),
            ("CommunityStandards.Templates.security-policy.md",  CommunityDocumentType.SecurityPolicy, "Политика безопасности"),
            ("CommunityStandards.Templates.legal.md",            CommunityDocumentType.Legal,          "Правовая информация")
        ];

        private readonly Lazy<IReadOnlyList<CommunityDocumentTemplate>> _templates = new(Load);

        public IReadOnlyList<CommunityDocumentTemplate> GetAll() => _templates.Value;

        private static IReadOnlyList<CommunityDocumentTemplate> Load()
        {
            Assembly assembly = typeof(CommunityDocumentTemplateProvider).Assembly;
            string assemblyName = assembly.GetName().Name!;

            List<CommunityDocumentTemplate> templates = [];
            List<string> missing = [];

            foreach ((string resourceSuffix, CommunityDocumentType type, string title) in Definitions)
            {
                string fullName = $"{assemblyName}.{resourceSuffix}";
                using Stream? resource = assembly.GetManifestResourceStream(fullName);

                if (resource is null)
                {
                    missing.Add(fullName);
                    continue;
                }

                using var reader = new StreamReader(resource);
                templates.Add(new CommunityDocumentTemplate(type, title, reader.ReadToEnd()));
            }

            // A missing resource is a packaging fault, not a runtime condition: the texts ship inside
            // this assembly. Degrading to a short list would hand clients a templates response that
            // silently omits document types, so fail loudly instead. Lazy caches the exception, so the
            // fault keeps surfacing rather than being papered over by a later call.
            if (missing.Count > 0)
            {
                throw new InvalidOperationException(
                    "Community document templates are missing from the assembly, so the templates "
                    + "endpoint cannot serve one per document type. Check the EmbeddedResource entries "
                    + $"in DevStart.Infrastructure.csproj for: {string.Join(", ", missing)}");
            }

            return templates;
        }
    }
}
