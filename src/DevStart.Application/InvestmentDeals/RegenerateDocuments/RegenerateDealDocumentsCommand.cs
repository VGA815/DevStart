using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.InvestmentDeals.RegenerateDocuments
{
    public sealed record RegenerateDealDocumentsCommand(Guid DealId) : ICommand;
}
