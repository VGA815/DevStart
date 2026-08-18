namespace DevStart.Domain.StartupPatents
{
    /// <summary>
    /// Kind of registered IP object. Its own enumeration rather than a flag on the record, because
    /// each kind has its own number format and its own section of the register: an invention is a
    /// seven-digit patent number, a computer program is a ten-digit "year + sequence" certificate.
    /// Values are append-only — they are persisted in <c>startup_patents.kind</c> and in the
    /// registry table.
    /// </summary>
    public enum IntellectualPropertyKind
    {
        /// <summary>Изобретение — patent, seven digits.</summary>
        Invention = 0,

        /// <summary>Полезная модель — patent, five to seven digits.</summary>
        UtilityModel = 1,

        /// <summary>Промышленный образец — patent, five to seven digits.</summary>
        IndustrialDesign = 2,

        /// <summary>Программа для ЭВМ — certificate, ten digits ("year + sequence").</summary>
        ComputerProgram = 3,

        /// <summary>База данных — certificate, same ten-digit shape as a program.</summary>
        Database = 4,

        /// <summary>Товарный знак — five to seven digits.</summary>
        Trademark = 5,
    }
}
