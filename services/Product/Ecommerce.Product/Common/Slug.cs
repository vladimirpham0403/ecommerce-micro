using System.Text;
using System.Text.RegularExpressions;

namespace Ecommerce.Product.Common;

public static class Slug
{
    public static string GenerateSlug(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Convert to lowercase
        input = input.ToLowerInvariant();

        // đ/Đ không bị NormalizationForm.FormD tách dấu -> xử lý tay trước khi bỏ dấu
        input = input.Replace('đ', 'd');

        // Remove diacritics (accents) from Latin characters
        input = RemoveDiacritics(input);

        // Replace spaces and invalid characters with hyphens
        input = Regex.Replace(input, @"[^a-z0-9\s-]", ""); // Allow only alphanumeric, spaces, and hyphens
        input = Regex.Replace(input, @"\s+", "-");         // Replace spaces with hyphens
        input = Regex.Replace(input, @"-+", "-").Trim('-'); // gộp gạch nối + cắt gạch nối thừa 2 đầu

        return input;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}
