using System.Globalization;
using System.Text.RegularExpressions;

namespace SistemaInstituto.Utilities
{
    public static class TextNormalizer
    {
        private static readonly CultureInfo Culture = new("es-AR");

        public static string ToTitleCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalizedSpaces = Regex.Replace(value.Trim(), @"\s+", " ");
            var titleCased = Culture.TextInfo.ToTitleCase(normalizedSpaces.ToLower(Culture));
            return Regex.Replace(
                titleCased,
                @"\b(\d+)(St|Nd|Rd|Th)\b",
                match => match.Groups[1].Value + match.Groups[2].Value.ToLower(Culture));
        }

        public static string? ToTitleCaseOrNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return ToTitleCase(value);
        }

        public static string ToSentenceCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalizedSpaces = Regex.Replace(value.Trim(), @"\s+", " ");
            var lowerCased = normalizedSpaces.ToLower(Culture);
            return Culture.TextInfo.ToUpper(lowerCased[0]) + lowerCased[1..];
        }
    }
}
