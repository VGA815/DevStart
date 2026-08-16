namespace DevStart.Application.Abstractions.Captcha
{
    /// <summary>
    /// The verdict of a captcha check. Three states rather than a bool: "the provider could not be
    /// reached" is deliberately distinct from "the provider said this is a bot", because the two get
    /// different HTTP responses and only the former is configurable (see CaptchaOptions.FailOpen).
    /// </summary>
    public enum CaptchaOutcome
    {
        /// <summary>The provider validated the token.</summary>
        Human = 0,

        /// <summary>The provider explicitly rejected the token. Always fail-closed.</summary>
        Bot = 1,

        /// <summary>The provider errored, timed out, or answered unintelligibly.</summary>
        Unavailable = 2,
    }
}
