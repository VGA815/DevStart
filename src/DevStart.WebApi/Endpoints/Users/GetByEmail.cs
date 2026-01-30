using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Users.GetByEmail;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class GetByEmail : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("users", async (
                [FromQuery] string email, 
                IQueryHandler<GetUserByEmailQuery, UserResponse> handler, 
                CancellationToken cancellationToken) =>
            {
                var query = new GetUserByEmailQuery(email);

                Result<UserResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.Users);
        }
    }
}