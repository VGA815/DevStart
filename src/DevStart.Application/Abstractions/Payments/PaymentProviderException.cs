using System;

namespace DevStart.Application.Abstractions.Payments
{
    /// <summary>
    /// Thrown by an <see cref="IPaymentProvider"/> implementation when a payment operation cannot be
    /// completed. <see cref="IsTransient"/> is <c>true</c> for network/timeout/5xx failures that may
    /// succeed on retry (the caller maps these to 503 Service Unavailable); <c>false</c> for a
    /// definitive rejection or malformed response from the provider (mapped to 400).
    /// </summary>
    public sealed class PaymentProviderException : Exception
    {
        public PaymentProviderException(string message, bool isTransient, Exception? innerException = null)
            : base(message, innerException)
        {
            IsTransient = isTransient;
        }

        public bool IsTransient { get; }
    }
}
