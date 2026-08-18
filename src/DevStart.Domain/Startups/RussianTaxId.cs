namespace DevStart.Domain.Startups
{
    /// <summary>
    /// Local check-digit validation for ИНН and ОГРН (SC-66). Both carry a checksum, so a typo is
    /// caught here — instantly, offline, before any external lookup is worth making. It says nothing
    /// about whether the number belongs to anyone in particular: a well-formed ИНН of somebody else's
    /// company passes this check, which is exactly why the platform never words the result as
    /// confirmed ownership.
    /// </summary>
    public static class RussianTaxId
    {
        private static readonly int[] Inn10 = [2, 4, 10, 3, 5, 9, 4, 6, 8];
        private static readonly int[] Inn12First = [7, 2, 4, 10, 3, 5, 9, 4, 6, 8];
        private static readonly int[] Inn12Second = [3, 7, 2, 4, 10, 3, 5, 9, 4, 6, 8];

        /// <summary>Digits only, or <c>null</c> when the input holds anything else.</summary>
        public static string? Normalize(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            string trimmed = raw.Trim();
            foreach (char c in trimmed)
            {
                if (!char.IsDigit(c))
                {
                    return null;
                }
            }

            return trimmed;
        }

        /// <summary>Ten-digit (organisation) or twelve-digit (sole trader) ИНН with a valid checksum.</summary>
        public static bool IsValidInn(string? raw)
        {
            string? digits = Normalize(raw);

            // The first two digits are the tax region and are never "00". Worth checking explicitly:
            // an all-zero placeholder satisfies the checksum arithmetic, and open-data dumps carry
            // such placeholders where an ИНН is unknown. Storing one would produce a comparison
            // against a number that identifies nobody.
            if (digits is null || digits.StartsWith("00", StringComparison.Ordinal))
            {
                return false;
            }

            return digits.Length switch
            {
                10 => Check(digits, Inn10, 9),
                12 => Check(digits, Inn12First, 10) && Check(digits, Inn12Second, 11),
                _ => false,
            };
        }

        /// <summary>Thirteen-digit ОГРН or fifteen-digit ОГРНИП with a valid checksum.</summary>
        public static bool IsValidOgrn(string? raw)
        {
            string? digits = Normalize(raw);

            // The leading digit is the record's attribute: 1/5 for a legal entity, 2 for a state
            // registration, 3/4 for a sole trader. Zero is not among them, so an all-zero placeholder
            // is rejected before the arithmetic (which it would otherwise satisfy).
            if (digits is null || digits[0] is < '1' or > '5')
            {
                return false;
            }

            // The control digit is the last digit of the leading part divided by 11 (13-digit ОГРН) or
            // by 13 (15-digit ОГРНИП).
            (int length, int divisor) = digits.Length switch
            {
                13 => (13, 11),
                15 => (15, 13),
                _ => (0, 0),
            };

            if (length == 0 || !long.TryParse(digits[..(length - 1)], out long leading))
            {
                return false;
            }

            return (char)('0' + (int)(leading % divisor % 10)) == digits[length - 1];
        }

        private static bool Check(string digits, int[] weights, int controlIndex)
        {
            int sum = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                sum += weights[i] * (digits[i] - '0');
            }

            return sum % 11 % 10 == digits[controlIndex] - '0';
        }
    }
}
