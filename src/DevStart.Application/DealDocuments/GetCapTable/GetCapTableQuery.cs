using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.DealDocuments.Generation;

namespace DevStart.Application.DealDocuments.GetCapTable
{
    public sealed record GetCapTableQuery(Guid DealId) : IQuery<CapTableResult>;
}
