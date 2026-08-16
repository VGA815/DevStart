namespace DevStart.Application.Configuration
{
    /// <summary>
    /// Yandex SmartCaptcha tunables. Lives in Application (not Infrastructure, where
    /// <c>TwoFactorOptions</c> sits) because both the Infrastructure verifier and the WebApi endpoint
    /// filter read it, and neither of those may depend on the other. Bound from the "Captcha" section
    /// by Infrastructure, the same way <see cref="TrustedDeviceOptions"/> is.
    /// </summary>
    public sealed class CaptchaOptions
    {
        public const string SectionName = "Captcha";

        /// <summary>
        /// Global kill switch. When false the endpoint filter short-circuits: no token is required and
        /// no outbound call is made. Defaults to false so a fresh clone, <c>dotnet run</c>, CI and the
        /// integration tests all boot without a Yandex Cloud account.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Server-side secret from the Yandex Cloud captcha resource. Never reaches the browser — the
        /// browser gets the separate, public client (site) key baked into the frontend bundle.
        /// </summary>
        public string ServerKey { get; set; } = string.Empty;

        public string ValidateUrl { get; set; } = "https://smartcaptcha.yandexcloud.net/validate";

        /// <summary>Deliberately short: a login must not hang waiting on a third party.</summary>
        public int TimeoutSeconds { get; set; } = 3;

        /// <summary>
        /// When the provider errors or times out, let the request through (Yandex's own guidance) —
        /// an outage at the captcha vendor should not lock everyone out of the product. An explicit
        /// rejection is a hard failure regardless of this flag.
        /// </summary>
        public bool FailOpen { get; set; } = true;

        /// <summary>
        /// Rollout aid: verify and log the outcome but never block. Lets you measure the real bot/human
        /// split and confirm tokens are arriving before enforcing. Ignored when Enabled is false.
        /// </summary>
        public bool ShadowMode { get; set; }
    }
}
