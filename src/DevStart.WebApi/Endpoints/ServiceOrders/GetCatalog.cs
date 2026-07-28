using DevStart.Application.ServiceOrders;
using DevStart.Domain.ServiceOrders;
using Microsoft.Extensions.Options;

namespace DevStart.WebApi.Endpoints.ServiceOrders
{
    /// <summary>
    /// SC-49: the one-time service catalog (fixed prices, configured in the "Services" section).
    /// Anonymous so the plans page can show prices before sign-in; buying still requires the
    /// service_orders::checkout permission.
    /// </summary>
    internal sealed class GetCatalog : IEndpoint
    {
        public sealed record Item(ServiceType ServiceType, decimal Price, string Currency, string Description);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/service-orders/catalog", (IOptions<ServiceCatalogOptions> catalogOptions) =>
            {
                List<Item> items = catalogOptions.Value.Items
                    .Where(i => i.Price > 0m)
                    .Select(i => new Item(i.ServiceType, i.Price, i.Currency, i.Description))
                    .ToList();

                return Results.Ok(items);
            })
                .WithTags(Tags.ServiceOrders)
                .AllowAnonymous();
        }
    }
}
