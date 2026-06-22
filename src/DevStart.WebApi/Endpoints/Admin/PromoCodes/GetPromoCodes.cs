using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.PromoCodes.GetPromoCodes;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.PromoCodes
{
    internal sealed class GetPromoCodes : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/admin/promo-codes", async (
                IQueryHandler<GetPromoCodesQuery, List<PromoCodeResponse>> handler,
                CancellationToken cancellationToken,
                [FromQuery] bool? activeOnly = null,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 50) =>
            {
                var query = new GetPromoCodesQuery(activeOnly, pageNumber, pageSize);
                Result<List<PromoCodeResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminPromoCodesRead)
                .WithTags(Tags.Admin);
        }
    }
}
