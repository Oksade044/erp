using System.Text;

namespace ERP.Domain.Common;

/// <summary>
/// Axtarış üçün mətn normalizasiyası və uyğunluq sıralaması (bütün modullarda ortaq — DRY).
/// Böyük/kiçik hərfə həssas deyil və Azərbaycan hərflərini ASCII qarşılığına çevirir
/// (ç→c, ş→s, ğ→g, ö→o, ü→u, ı/İ→i, ə→e), belə ki "lica" yazanda "liça" tapılır.
/// </summary>
public static class SearchNormalizer
{
    /// <summary>Uyğunluq yoxdur (sıralamada ən sonda).</summary>
    public const int NoMatch = int.MaxValue;

    /// <summary>Mətni kiçik hərfli, diakritiksiz (ASCII-yə yaxın) formaya salır.</summary>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var sb = new StringBuilder(input.Length);
        foreach (var ch in input.Trim())
        {
            var mapped = ch switch
            {
                'ç' or 'Ç' => 'c',
                'ş' or 'Ş' => 's',
                'ğ' or 'Ğ' => 'g',
                'ö' or 'Ö' => 'o',
                'ü' or 'Ü' => 'u',
                'ı' or 'I' => 'i',
                'İ' or 'i' => 'i',
                'ə' or 'Ə' => 'e',
                'é' or 'É' => 'e',
                _ => char.ToLowerInvariant(ch)
            };
            sb.Append(mapped);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Verilmiş (artıq normalizə olunmuş) axtarış termininə görə bir yazının uyğunluq balı.
    /// Kiçik = daha yaxşı. 0 = tam uyğun, 1 = tam söz, 2 = sözün əvvəli, 3 = daxilində,
    /// 4 = ikinci dərəcəli sahələrdə (SKU/kateqoriya və s.), NoMatch = tapılmadı.
    /// </summary>
    public static int Score(string normalizedTerm, string? primaryText, IEnumerable<string?>? secondaryTexts = null)
    {
        if (string.IsNullOrEmpty(normalizedTerm)) return 0;

        var name = Normalize(primaryText);
        if (name.Length > 0)
        {
            if (name == normalizedTerm) return 0;

            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (Array.IndexOf(words, normalizedTerm) >= 0) return 1;

            foreach (var w in words)
                if (w.StartsWith(normalizedTerm, StringComparison.Ordinal)) return 2;

            if (name.StartsWith(normalizedTerm, StringComparison.Ordinal)) return 2;
            if (name.Contains(normalizedTerm, StringComparison.Ordinal)) return 3;
        }

        if (secondaryTexts is not null)
            foreach (var s in secondaryTexts)
                if (Normalize(s).Contains(normalizedTerm, StringComparison.Ordinal))
                    return 4;

        return NoMatch;
    }

    /// <summary>Mətn axtarış termininə hər hansı şəkildə uyğun gəlirmi (süzgəc üçün).</summary>
    public static bool Matches(string normalizedTerm, string? primaryText, IEnumerable<string?>? secondaryTexts = null) =>
        Score(normalizedTerm, primaryText, secondaryTexts) != NoMatch;
}
