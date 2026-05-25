using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.EmailVerificationTokens.ResendEmailVerification;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.EmailVerificationTokens
{
    internal sealed class Resend : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/email-verification/resend", async (
                [FromQuery] string email, 
                ICommandHandler<ResendEmailVerificationCommand> innerHandler,
                CancellationToken cancellationToken) => 
            { 
                var command = new ResendEmailVerificationCommand(email);

                Result result = await innerHandler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .WithTags(Tags.EmailVerification)
                .RequireRateLimiting("auth");
        }
    }
}
