namespace DevStart.Domain.PatentRegistry
{
    /// <summary>
    /// State of legal protection as the register reports it. A terminated record stays in the dump
    /// with a changed status, which is why loading is an upsert and nothing is ever deleted (SC-63):
    /// a patent that lapsed must show as lapsed, not disappear.
    /// </summary>
    public enum PatentProtectionStatus
    {
        /// <summary>The dump carried a status this platform does not recognise, or none at all.</summary>
        Unknown = 0,

        /// <summary>Действует.</summary>
        Active = 1,

        /// <summary>Прекращён (срок истёк).</summary>
        Terminated = 2,

        /// <summary>Досрочно прекращён.</summary>
        EarlyTerminated = 3,
    }
}
