using System.Globalization;
using System.Text;

namespace CanvasArt.API.Models.Common;

public static class SlugHelper
{
    /// <summary>Produces a URL-safe, lowercase, hyphenated slug from arbitrary text.</summary>
    public static string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Strip diacritics.
        var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else if (c is ' ' or '-' or '_' or '.' or '/')
                sb.Append('-');
        }

        var slug = sb.ToString().Normalize(NormalizationForm.FormC);

        // Collapse repeated hyphens and trim.
        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");

        return slug.Trim('-');
    }
}
