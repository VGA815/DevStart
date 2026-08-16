using Microsoft.Extensions.Options;

namespace DevStart.IntegrationTests.Fakes
{
    /// <summary>
    /// An <see cref="IOptionsMonitor{T}"/> that hands back one shared instance, so a test can mutate
    /// the options object between requests and have the change take effect immediately. This is what
    /// lets <c>CaptchaTests</c> flip Captcha:Enabled around a single call while every other test class
    /// keeps the captcha switched off.
    /// </summary>
    internal sealed class MutableOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = value;

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
