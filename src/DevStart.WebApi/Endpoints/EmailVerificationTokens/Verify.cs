using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.EmailVerificationTokens.VerifyEmail;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.EmailVerificationTokens
{
    internal sealed class Verify : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/verify", async (
                [FromQuery] Guid token, 
                IQueryHandler<VerifyEmailQuery, EmailVerificationResponse> innerHandler, 
                CancellationToken cancellationToken) => 
            { 
                var query = new VerifyEmailQuery(token);
                Result<EmailVerificationResponse> result = await innerHandler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.EmailVerification)
                .WithName("VerifyEmail");
        }
    }
}
