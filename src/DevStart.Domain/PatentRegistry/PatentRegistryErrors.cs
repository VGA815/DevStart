using DevStart.SharedKernel;

namespace DevStart.Domain.PatentRegistry
{
    public static class PatentRegistryErrors
    {
        /// <summary>
        /// The uploaded dump has a layout this platform cannot read. Parsing happens before anything is
        /// stored: a file whose columns we cannot find is not provenance for anything.
        /// </summary>
        public static Error UnreadableDataset(string detail) => Error.Validation(
            "PatentRegistry.UnreadableDataset",
            $"Не удалось разобрать выгрузку реестра: {detail}");

        /// <summary>
        /// The body outgrew the cap while being read. Distinct from the validator's check on the
        /// declared length: that one trusts what the client said the size was, this one is what
        /// actually arrived.
        /// </summary>
        public static Error DatasetTooLarge(long maxBytes) => Error.Validation(
            "PatentRegistry.DatasetTooLarge",
            $"Выгрузка больше допустимых {maxBytes / (1024 * 1024)} МБ. "
                + "Полный реестр загружается квартальным джобом по настроенному URL.");

        public static readonly Error EmptyDataset = Error.Validation(
            "PatentRegistry.EmptyDataset",
            "В выгрузке не нашлось ни одной пригодной записи.");
    }
}
