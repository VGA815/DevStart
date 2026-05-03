using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InvestmentApplications.Create;
using DevStart.Domain.InvestmentApplications;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.InvestmentApplications
{
    internal sealed class Create : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("startup_id")] Guid StartupId,
            [property: JsonPropertyName("roadmap_item_id")] Guid? RoadmapItemId,
            [property: JsonPropertyName("amount")] decimal Amount,
            [property: JsonPropertyName("message")] string? Message,
            [property: JsonPropertyName("instrument")] InvestmentInstrument Instrument = InvestmentInstrument.Safe,
            [property: JsonPropertyName("valuation_cap")] decimal? ValuationCap = null,
            [property: JsonPropertyName("discount")] decimal? Discount = null,
            [property: JsonPropertyName("interest_rate")] decimal? InterestRate = null,
            [property: JsonPropertyName("term_months")] int? TermMonths = null,
            [property: JsonPropertyName("pre_money_valuation")] decimal? PreMoneyValuation = null,
            [property: JsonPropertyName("liquidation_preference")] decimal LiquidationPreference = 1.0m,
            [property: JsonPropertyName("pro_rata_rights")] bool ProRataRights = false);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/investment-applications", async (
                [FromBody] Request request,
                ICommandHandler<CreateInvestmentApplicationCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateInvestmentApplicationCommand(
                    request.StartupId,
                    request.RoadmapItemId,
                    request.Amount,
                    request.Message,
                    request.Instrument,
                    request.ValuationCap,
                    request.Discount,
                    request.InterestRate,
                    request.TermMonths,
                    request.PreMoneyValuation,
                    request.LiquidationPreference,
                    request.ProRataRights);

                Result<Guid> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.InvestmentApplicationsCreate)
                .WithTags(Tags.InvestmentApplications);
        }
    }
}
