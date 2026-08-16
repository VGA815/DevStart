using DevStart.Application.Abstractions.Captcha;
using DevStart.Application.Configuration;
using DevStart.SharedKernel;
using Microsoft.Extensions.Options;

namespace DevStart.WebApi.Infrastructure
{
    /// <summary>
    /// Marks an endpoint as captcha-protected. Carried as endpoint metadata purely so
    /// <see cref="CaptchaHeaderOperationFilter"/> can document the header in OpenAPI — enforcement
    /// itself is done by <see cref="CaptchaEndpointFilter"/>.
    /// </summary>
    internal sealed class RequiresCaptchaMetadata;

    /// <summary>
    /// Requires a valid Yandex SmartCaptcha token in the X-Captcha-Token header.
    ///
    /// Runs as an endpoint filter rather than middleware or an Application decorator, which buys
    /// three things: the client IP has already been rewritten by UseForwardedHeaders; the rate
    /// limiter has already shed load, so the outbound call to Yandex is bounded per IP; and the
    /// request is rejected before the handler touches the database, which preserves the
    /// enumeration-safety that LoginUserCommandHandler works hard for.
    ///
    /// The token travels in a header, not the body, because two of the protected endpoints have no
    /// JSON body at all (email-verification/resend is query-only, oauth/{provider}/start is a GET),
    /// and because an endpoint filter runs after model binding — the body stream is already consumed
    /// and cannot be re-read.
    /// </summary>
    internal sealed class CaptchaEndpointFilter : IEndpointFilter
    {
        internal const string HeaderName = "X-Captcha-Token";

        private readonly IOptionsMonitor<CaptchaOptions> _options;
        private readonly ILogger<CaptchaEndpointFilter> _logger;

        // Only singletons belong in this constructor. AddEndpointFilter<T>() builds ONE instance per
        // endpoint from the root provider at startup, so injecting ICaptchaVerifier here would capture
        // a single typed HttpClient for the life of the process and defeat handler rotation. It is
        // resolved per request from HttpContext.RequestServices instead.
        //
        // IOptionsMonitor rather than IOptions for the same reason: IOptions would freeze Enabled at
        // boot, and the whole point of the flag is that it can be flipped without a rebuild.
        public CaptchaEndpointFilter(
            IOptionsMonitor<CaptchaOptions> options,
            ILogger<CaptchaEndpointFilter> logger)
        {
            _options = options;
            _logger = logger;
        }

        public async ValueTask<object?> InvokeAsync(
            EndpointFilterInvocationContext context,
            EndpointFilterDelegate next)
        {
            CaptchaOptions options = _options.CurrentValue;

            if (!options.Enabled)
            {
                return await next(context);
            }

            HttpContext http = context.HttpContext;
            string? token = http.Request.Headers[HeaderName].FirstOrDefault();
            string? ip = http.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogInformation(
                    "Captcha token missing on {Path} from {ClientIp}.", http.Request.Path, ip);

                // No token means nothing to validate — do not spend an outbound call on it.
                return options.ShadowMode
                    ? await next(context)
                    : CustomResults.Problem(Result.Failure(CaptchaErrors.Missing));
            }

            ICaptchaVerifier verifier = http.RequestServices.GetRequiredService<ICaptchaVerifier>();
            CaptchaOutcome outcome = await verifier.VerifyAsync(token, ip, http.RequestAborted);

            if (options.ShadowMode)
            {
                _logger.LogInformation(
                    "Captcha shadow mode: {Outcome} on {Path} from {ClientIp}.",
                    outcome, http.Request.Path, ip);
                return await next(context);
            }

            return outcome switch
            {
                CaptchaOutcome.Human => await next(context),
                CaptchaOutcome.Bot => CustomResults.Problem(Result.Failure(CaptchaErrors.Failed)),
                // Unavailable: fail open by default so a vendor outage does not take auth down with it.
                _ => options.FailOpen
                    ? await next(context)
                    : CustomResults.Problem(Result.Failure(CaptchaErrors.Unavailable)),
            };
        }
    }
}
