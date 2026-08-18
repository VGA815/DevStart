using DevStart.SharedKernel;

namespace DevStart.Domain.StartupPatents
{
    public static class StartupPatentErrors
    {
        public static Error NotFound(Guid patentId) => Error.NotFound(
            "StartupPatents.NotFound",
            $"The startup IP record with id = '{patentId}' was not found.");

        public static readonly Error Unauthorized = Error.Problem(
            "StartupPatents.Unauthorized",
            "You are not allowed to perform this action on this startup's IP records.");

        public static readonly Error DuplicateNumber = Error.Conflict(
            "StartupPatents.DuplicateNumber",
            "This registration number is already listed for this startup.");

        public static readonly Error LimitReached = Error.Problem(
            "StartupPatents.LimitReached",
            $"A startup can list at most {StartupPatent.MaxPerStartup} IP records.");

        /// <summary>
        /// The number does not have the shape its kind is issued in. Deliberately distinct from a
        /// number that simply is not in the register: one is a typo at input, the other is a statement
        /// about the record, and conflating them would tell the founder the wrong thing.
        /// </summary>
        public static Error InvalidNumber(IntellectualPropertyKind kind) => Error.Validation(
            "StartupPatents.InvalidNumber",
            $"Номер не соответствует виду объекта: ожидается {StartupPatent.NumberFormatHint(kind)}.");
    }
}
