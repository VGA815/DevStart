using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Messages.Create;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Messages
{
    internal sealed class Send : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/messages", async (
                CreateMessageCommand command,
                ICommandHandler<CreateMessageCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                Result<Guid> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.MessagesSend)
                .WithTags(Tags.Messages);
        }
    }
}
