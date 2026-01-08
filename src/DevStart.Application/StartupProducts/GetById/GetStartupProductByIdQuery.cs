using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupProducts.GetById
{
    public sealed record GetStartupProductByIdQuery(Guid StartupId) : IQuery<StartupProductResponse>;
}
