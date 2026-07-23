using DevStart.Domain.StartupCommunityStandards;

namespace DevStart.Application.CommunityStandards
{
    /// <summary>Platform-provided starter text for a community document — the analogue of GitHub offering
    /// the Contributor Covenant when you click "Add" on a code of conduct.</summary>
    public sealed record CommunityDocumentTemplate(CommunityDocumentType Type, string Title, string Content);

    /// <summary>
    /// Serves the starter templates. Declared here and implemented in Infrastructure (the texts ship as
    /// embedded Markdown resources), the same arrangement as <c>IValuationBenchmarkProvider</c>.
    /// </summary>
    public interface ICommunityDocumentTemplateProvider
    {
        IReadOnlyList<CommunityDocumentTemplate> GetAll();
    }
}
