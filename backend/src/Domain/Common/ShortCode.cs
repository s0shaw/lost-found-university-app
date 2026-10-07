using System.Globalization;
using System.Security.Cryptography;

namespace UniversityLostFound.Domain.Common;

public static class ShortCode
{
    public const int BodyLength = 6;

    public static int LengthFor(string prefix) => prefix.Length + BodyLength + 1;

    public static string Generate(string prefix) =>
        prefix + "-" + RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);

    public static bool IsValid(string? value, string prefix)
    {
        if (value is null || value.Length != prefix.Length + BodyLength + 1)
        {
            return false;
        }

        if (!value.StartsWith(prefix, StringComparison.Ordinal) || value[prefix.Length] != '-')
        {
            return false;
        }

        for (var i = prefix.Length + 1; i < value.Length; i++)
        {
            if (!char.IsAsciiDigit(value[i]))
            {
                return false;
            }
        }

        return true;
    }
}
