using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InvestorProfiles.Create;
using DevStart.Domain.Investors;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.InvestorProfiles
{
    internal sealed class Create : IEndpoint
    {
        /// <summary>
        /// Everything except <c>type</c> and <c>avatar_id</c> is stored on the shared Profile — the same
        /// fields <c>GET api/investor-profiles/{userId}</c> returns. They must stay on this contract:
        /// unknown JSON members are dropped silently, so omitting one loses the user's input without an
        /// error. <c>avatar_id</c> — логотип фонда; для физлица игнорируется.
        /// </summary>
        public sealed record Request(
            [property: JsonPropertyName("type")] InvestorProfileType Type,
            [property: JsonPropertyName("display_name")] string DisplayName,
            [property: JsonPropertyName("bio")] string? Bio,
            [property: JsonPropertyName("website")] string? Website,
            [property: JsonPropertyName("is_public")] bool IsPublic,
            [property: JsonPropertyName("avatar_id")] Guid? AvatarId);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/investor-profiles", async (
                [FromBody] Request request,
                ICommandHandler<CreateInvestorProfileCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateInvestorProfileCommand(
                    request.Type,
                    request.DisplayName ?? string.Empty,
                    request.Bio,
                    request.Website,
                    request.IsPublic,
                    request.AvatarId);

                Result<Guid> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.InvestorProfilesCreate)
                .WithTags(Tags.InvestorProfiles);
        }
    }
}
