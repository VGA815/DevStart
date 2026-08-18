using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.PatentRegistry.RunPatentRegistryImport;
using DevStart.Application.Admin.PatentRegistry.UploadPatentRegistryDataset;
using DevStart.Domain.StartupPatents;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.PatentRegistry
{
    /// <summary>
    /// The two ways rows get into the local register copy: an uploaded dump and a queued refresh.
    /// Nothing else writes there, and neither of these touches a score or a valuation.
    /// </summary>
    internal sealed class PatentRegistryEndpoints : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/admin/patent-registry/upload", async (
                IFormFile file,
                [FromQuery] IntellectualPropertyKind kind,
                ICommandHandler<UploadPatentRegistryDatasetCommand, UploadPatentRegistryDatasetResponse> handler,
                CancellationToken cancellationToken) =>
            {
                // The cap is enforced where the bytes are actually read (the handler), not here:
                // file.Length is only what the client claims, and IFormFile has no capped read.
                await using Stream stream = file.OpenReadStream();

                var command = new UploadPatentRegistryDatasetCommand(
                    stream, file.Length, file.FileName, file.ContentType, kind);

                Result<UploadPatentRegistryDatasetResponse> result =
                    await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .DisableAntiforgery()
                .HasPermission(Permissions.AdminPatentRegistryManage)
                .WithTags(Tags.Admin);

            app.MapPost("api/admin/patent-registry/import", async (
                ICommandHandler<RunPatentRegistryImportCommand> handler,
                CancellationToken cancellationToken) =>
            {
                Result result = await handler.Handle(new RunPatentRegistryImportCommand(), cancellationToken);
                return result.Match(() => Results.Accepted(), CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminPatentRegistryManage)
                .WithTags(Tags.Admin);
        }
    }
}
