
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupProducts.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.StartupProduct
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("startups/products/{productId:guid}", async (
                Guid productId, 
                IQueryHandler<GetStartupProductByIdQuery, StartupProductResponse> handler, 
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupProductByIdQuery(productId);

                Result<StartupProductResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.StartupProducts);
        }
    }
}
