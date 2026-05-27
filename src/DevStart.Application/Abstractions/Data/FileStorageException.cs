using System;

namespace DevStart.Application.Abstractions.Data
{
    /// <summary>
    /// Thrown by an <see cref="IFileStorage"/> implementation when an object-storage operation fails.
    /// <see cref="NotFound"/> is <c>true</c> when the requested object does not exist (the caller maps
    /// this to 404); otherwise the failure is treated as a transient storage outage (mapped to 503).
    /// </summary>
    public sealed class FileStorageException : Exception
    {
        public FileStorageException(string message, bool notFound, Exception? innerException = null)
            : base(message, innerException)
        {
            NotFound = notFound;
        }

        public bool NotFound { get; }
    }
}
