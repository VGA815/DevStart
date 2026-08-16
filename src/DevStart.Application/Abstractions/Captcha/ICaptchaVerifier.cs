namespace DevStart.Application.Abstractions.Captcha
{
    /// <summary>
    /// Validates a client-supplied captcha token against the provider. Implementations never throw
    /// for transport failures — an unreachable provider is reported as
    /// <see cref="CaptchaOutcome.Unavailable"/>, mirroring how <c>IPaymentProvider</c> degrades.
    /// </summary>
    public interface ICaptchaVerifier
    {
        /// <param name="token">The single-use token minted by the browser widget.</param>
        /// <param name="clientIp">
        /// The caller's IP, forwarded to the provider as an extra signal. Null when it cannot be
        /// determined; the check still works without it.
        /// </param>
        Task<CaptchaOutcome> VerifyAsync(string token, string? clientIp, CancellationToken cancellationToken);
    }
}
