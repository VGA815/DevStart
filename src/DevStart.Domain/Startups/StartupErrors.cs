using DevStart.SharedKernel;

namespace DevStart.Domain.Startups
{
    public static class StartupErrors
    {
        public static Error NotFound(Guid startupId) => Error.NotFound(
            "Startups.NotFound",
            $"The startup with the Id = '{startupId}' was not found");
        public static readonly Error NotFoundByName = Error.NotFound(
            "Startups.NotFoundByName",
            "The startup with the specified name was not found");
        public static readonly Error NameNotUnique = Error.Conflict(
            "Startups.NameNotUnique",
            "The provided name is not unique");
        public static readonly Error UserAlreadyMember = Error.Conflict(
            "Startups.UserAlreadyMember",
            "The user is already a member of the startup");
        public static readonly Error AlreadyBanned = Error.Conflict(
            "Startups.AlreadyBanned",
            "The startup is already banned");
        public static readonly Error NotBanned = Error.Conflict(
            "Startups.NotBanned",
            "The startup is not banned");
        public static readonly Error BanExpiryInPast = Error.Validation(
            "Startups.BanExpiryInPast",
            "The ban expiry date must be in the future");

        /// <summary>
        /// The declared ИНН fails its own check digit — a typo, caught locally before any external
        /// lookup. A valid ИНН is still only a claim (see <see cref="RussianTaxId"/>).
        /// </summary>
        public static readonly Error InvalidInn = Error.Validation(
            "Startups.InvalidInn",
            "ИНН должен состоять из 10 цифр (для организации) или 12 (для ИП) и проходить проверку контрольной суммы");

        public static readonly Error InvalidOgrn = Error.Validation(
            "Startups.InvalidOgrn",
            "ОГРН должен состоять из 13 цифр (или 15 для ОГРНИП) и проходить проверку контрольной суммы");
    }
}
