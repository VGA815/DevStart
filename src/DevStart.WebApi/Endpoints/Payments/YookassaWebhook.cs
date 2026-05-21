using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Payments.Webhooks;
using DevStart.Infrastructure.Payments;
using DevStart.SharedKernel;
using Microsoft.Extensions.Options;

namespace DevStart.WebApi.Endpoints.Payments
{
    internal sealed class YookassaWebhook : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/webhooks/yookassa", async (
                HttpRequest request,
                ICommandHandler<HandleYookassaWebhookCommand> handler,
                IOptions<YooKassaOptions> options,
                IHostEnvironment env,
                CancellationToken cancellationToken) =>
            {
                if (!env.IsDevelopment() && options.Value.VerifyWebhookIp)
                {
                    string? remote = request.HttpContext.Connection.RemoteIpAddress?.ToString();
                    if (string.IsNullOrEmpty(remote) || !IpAllowList.IsAllowed(remote, options.Value.AllowedIps))
                    {
                        return Results.StatusCode(StatusCodes.Status403Forbidden);
                    }
                }

                using var reader = new StreamReader(request.Body);
                string body = await reader.ReadToEndAsync(cancellationToken);

                Result result = await handler.Handle(new HandleYookassaWebhookCommand(body), cancellationToken);


                if (result.IsSuccess)
                {
                    return Results.Ok();
                }
                if (result.Error.Code == "Payments.WebhookPayloadInvalid")
                {
                    return Results.BadRequest();
                }
                return Results.Ok();
            })
                .AllowAnonymous()
                .WithTags(Tags.Payments);
        }
    }

    internal static class IpAllowList
    {
        public static bool IsAllowed(string remoteIp, string[] allowed)
        {
            if (allowed is null || allowed.Length == 0) return true;
            foreach (string entry in allowed)
            {
                if (string.IsNullOrWhiteSpace(entry)) continue;
                if (entry.Contains('/'))
                {
                    if (IsInCidr(remoteIp, entry)) return true;
                }
                else if (string.Equals(entry, remoteIp, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsInCidr(string ip, string cidr)
        {
            string[] parts = cidr.Split('/');
            if (parts.Length != 2) return false;
            if (!System.Net.IPAddress.TryParse(parts[0], out System.Net.IPAddress? network)) return false;
            if (!System.Net.IPAddress.TryParse(ip, out System.Net.IPAddress? address)) return false;
            if (!int.TryParse(parts[1], out int prefixLength)) return false;
            byte[] networkBytes = network.GetAddressBytes();
            byte[] addressBytes = address.GetAddressBytes();
            if (networkBytes.Length != addressBytes.Length) return false;
            int fullBytes = prefixLength / 8;
            int remainingBits = prefixLength % 8;
            for (int i = 0; i < fullBytes; i++)
            {
                if (networkBytes[i] != addressBytes[i]) return false;
            }
            if (remainingBits == 0) return true;
            int mask = 0xFF << (8 - remainingBits) & 0xFF;
            return (networkBytes[fullBytes] & mask) == (addressBytes[fullBytes] & mask);
        }
    }
}
