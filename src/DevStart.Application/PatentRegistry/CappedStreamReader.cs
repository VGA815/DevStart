namespace DevStart.Application.PatentRegistry
{
    /// <summary>
    /// Reads a stream into memory under a hard byte cap, refusing the moment it is exceeded.
    ///
    /// One implementation for both ways a dump arrives — the admin upload and the job's download —
    /// because both face the same thing: a declared size is a claim by whoever sent the bytes. An
    /// understated Content-Length, a chunked response that announces nothing, or a small ZIP that
    /// expands into gigabytes all end the same way, and none of them may be allowed to decide how much
    /// memory this server spends.
    /// </summary>
    public static class CappedStreamReader
    {
        private const int ChunkBytes = 81920;

        /// <summary>
        /// One wording for "too large", shared with the callers that can refuse earlier — on a declared
        /// Content-Length or a ZIP entry's declared size. The same refusal read two different ways
        /// would look like two different problems.
        /// </summary>
        public static string TooLargeMessage(long cap) => $"Выгрузка больше допустимых {cap} байт.";

        /// <exception cref="InvalidDataException">The stream carried more than <paramref name="cap"/> bytes.</exception>
        public static async Task<byte[]> ReadAsync(Stream stream, long cap, CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            byte[] chunk = new byte[ChunkBytes];

            int read;
            while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
            {
                // Checked before the write, so the cap is a ceiling on what is held, not on what has
                // already been held once.
                if (buffer.Length + read > cap)
                {
                    throw new InvalidDataException(TooLargeMessage(cap));
                }

                buffer.Write(chunk, 0, read);
            }

            return buffer.ToArray();
        }
    }
}
