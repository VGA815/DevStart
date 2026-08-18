using DevStart.Application.Abstractions.Registry;

namespace DevStart.Infrastructure.Registry
{
    /// <summary>
    /// The default ЕГРЮЛ implementation: none. It answers "unavailable" for every ИНН, which the UI
    /// renders as "проверка по ЕГРЮЛ недоступна" — a statement about the platform, never about the
    /// startup.
    ///
    /// Wiring a real source is a separate, deliberate step: every provider is a paid or rate-limited
    /// service with its own contract, and a client written against a guessed one would be unverifiable.
    /// Everything else in SC-66 works without it — the check digit is local, and the ИНН comparison
    /// against a rightsholder happens inside the register copy this platform already holds.
    /// </summary>
    internal sealed class UnavailableLegalEntityRegistry : ILegalEntityRegistry
    {
        public Task<LegalEntityLookup> LookupAsync(string inn, CancellationToken cancellationToken) =>
            Task.FromResult(LegalEntityLookup.Unavailable);
    }
}
