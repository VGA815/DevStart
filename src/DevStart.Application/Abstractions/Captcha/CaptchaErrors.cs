using DevStart.SharedKernel;

namespace DevStart.Application.Abstractions.Captcha
{
    /// <summary>
    /// Unlike the other *Errors classes, these live in Application rather than Domain: a captcha is
    /// not an aggregate, and the domain has no business knowing about an edge anti-automation
    /// control. The "Aggregate.PascalCase" code convention is kept so clients can branch on the
    /// RFC 7807 <c>title</c> exactly as they do for every other error.
    /// </summary>
    public static class CaptchaErrors
    {
        /// <summary>No X-Captcha-Token header was sent. 400 — the client is simply not compliant.</summary>
        public static readonly Error Missing = Error.Problem(
            "Captcha.Missing",
            "A captcha token is required for this request.");

        /// <summary>The provider rejected the token. 400, and never fail-open: this is the signal.</summary>
        public static readonly Error Failed = Error.Problem(
            "Captcha.Failed",
            "The captcha check did not pass. Please try again.");

        /// <summary>
        /// The provider itself is down. 503, and only ever returned when Captcha:FailOpen is false —
        /// by default an unreachable provider lets the request through rather than taking login down
        /// with it.
        /// </summary>
        public static readonly Error Unavailable = Error.ServiceUnavailable(
            "Captcha.Unavailable",
            "The captcha service is temporarily unavailable. Please try again shortly.");
    }
}
