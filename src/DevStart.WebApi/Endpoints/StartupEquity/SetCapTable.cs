using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupEquity.SetCapTable;
using DevStart.Domain.StartupEquity;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.StartupEquity
{
    internal sealed class SetCapTable : IEndpoint
    {
        public sealed record HolderRequest(
            [property: JsonPropertyName("holder_type")] EquityHolderType HolderType,
            [property: JsonPropertyName("profile_id")] Guid? ProfileId,
            [property: JsonPropertyName("name")] string? Name,
            [property: JsonPropertyName("equity_percentage")] decimal EquityPercentage,
            [property: JsonPropertyName("vesting_start_date")] DateTime? VestingStartDate,
            [property: JsonPropertyName("vesting_months")] int? VestingMonths,
            [property: JsonPropertyName("cliff_months")] int? CliffMonths);

        public sealed record Request(
            [property: JsonPropertyName("holders")] IReadOnlyList<HolderRequest> Holders);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("api/startups/{startupId:guid}/cap-table", async (
                Guid startupId,
                [FromBody] Request request,
                ICommandHandler<SetStartupCapTableCommand> handler,
                CancellationToken cancellationToken) =>
            {
                IReadOnlyList<CapTableHolderInput> holders = (request.Holders ?? [])
                    .Select(h => new CapTableHolderInput(
                        h.HolderType,
                        h.ProfileId,
                        h.Name,
                        h.EquityPercentage,
                        h.VestingStartDate,
                        h.VestingMonths,
                        h.CliffMonths))
                    .ToList();

                var command = new SetStartupCapTableCommand(startupId, holders);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupEquityManage)
                .WithTags(Tags.StartupEquity);
        }
    }
}
