using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupProducts;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupProducts.GetById
{
    internal sealed class GetStartupProductByIdQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetStartupProductByIdQuery, StartupProductResponse>
    {
        public async Task<Result<StartupProductResponse>> Handle(GetStartupProductByIdQuery query, CancellationToken cancellationToken)
        {
            StartupProduct? startupProduct = await context.StartupProducts
                .SingleOrDefaultAsync(sp => sp.StartupId == query.StartupId, cancellationToken);

            if (startupProduct == null )
            {
                return Result.Failure<StartupProductResponse>(StartupProductErrors.NotFound(query.StartupId));
            }

            StartupProductResponse startupProductResponse = new StartupProductResponse
            {
                Differentiators = startupProduct.Differentiators,
                Problem = startupProduct.Problem,
                Solution = startupProduct.Solution,
                Stack = startupProduct.Stack,
                StartupId = query.StartupId,
                ValueProposition = startupProduct.ValueProposition,
            };

            return startupProductResponse;
        }
    }
}
