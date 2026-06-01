using System.Text.RegularExpressions;

namespace AuthService.Application.Helpers;

public static partial class PhoneNormalizer
{
    public static string Normalize(string phone)
    {
        var digits = DigitsOnly().Replace(phone, string.Empty);
        if (digits.Length == 11 && digits.StartsWith('8'))
            digits = "7" + digits[1..];
        if (digits.Length == 10)
            digits = "7" + digits;
        return digits;
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex DigitsOnly();
}
