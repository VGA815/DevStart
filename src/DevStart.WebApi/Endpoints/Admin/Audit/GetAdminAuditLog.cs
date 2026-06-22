using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Audit;
using DevStart.Domain.Admin;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.Audit
{
    internal sealed class GetAdminAuditLog : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/admin/audit", async (
                IQueryHandler<GetAdminAuditLogQuery, List<AdminAuditLogResponse>> handler,
                CancellationToken cancellationToken,
                [FromQuery] AdminTargetType? targetType = null,
                [FromQuery] Guid? targetId = null,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 50) =>
            {
                var query = new GetAdminAuditLogQuery(targetType, targetId, pageNumber, pageSize);
                Result<List<AdminAuditLogResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminAuditRead)
                .WithTags(Tags.Admin);
        }
    }
}
