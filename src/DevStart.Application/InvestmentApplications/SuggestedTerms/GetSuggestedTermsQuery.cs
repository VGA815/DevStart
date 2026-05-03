using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.InvestmentApplications;

namespace DevStart.Application.InvestmentApplications.SuggestedTerms
{
    public sealed record GetSuggestedTermsQuery(
        Guid StartupId,
        InvestmentInstrument Instrument,
        decimal Amount) : IQuery<SuggestedTermsResponse>;
}
