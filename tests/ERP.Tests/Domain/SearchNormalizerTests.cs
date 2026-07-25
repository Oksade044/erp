using ERP.Domain.Common;
using Xunit;

namespace ERP.Tests.Domain;

public class SearchNormalizerTests
{
    [Theory]
    [InlineData("KRALIÇA", "kralica")]
    [InlineData("Kraliça", "kralica")]
    [InlineData("Şüşə", "suse")]
    [InlineData("Ağ Stol", "ag stol")]
    [InlineData("Qızılı", "qizili")]
    [InlineData("Ömər", "omer")]
    public void Normalize_folds_azerbaijani_letters(string input, string expected) =>
        Assert.Equal(expected, SearchNormalizer.Normalize(input));

    // İstifadəçi "lica" (adi c ilə) yazanda "Kraliça" tapılmalıdır.
    [Fact]
    public void Diacritic_insensitive_match()
    {
        var term = SearchNormalizer.Normalize("lica");
        Assert.True(SearchNormalizer.Matches(term, "Kraliça Stol"));
    }

    [Theory]
    [InlineData("Kraliça")]
    [InlineData("kraliça")]
    [InlineData("KRALIÇA")]
    [InlineData("Kral")]
    [InlineData("Kr")]
    [InlineData("Kra")]
    [InlineData("liça")]
    [InlineData("lica")]
    public void Various_queries_find_kralica_stol(string query)
    {
        var term = SearchNormalizer.Normalize(query);
        Assert.True(SearchNormalizer.Matches(term, "Kraliça Stol"));
    }

    [Theory]
    [InlineData("stol")]
    [InlineData("Sto")]
    [InlineData("tol")]
    [InlineData("ol")]
    public void Partial_word_queries_find_stol(string query)
    {
        var term = SearchNormalizer.Normalize(query);
        Assert.True(SearchNormalizer.Matches(term, "Kraliça Stol"));
    }

    [Fact]
    public void Ranking_orders_exact_then_prefix_then_contains()
    {
        var term = SearchNormalizer.Normalize("stol");

        var exact = SearchNormalizer.Score(term, "Stol");          // tam uyğun → 0
        var wordPrefix = SearchNormalizer.Score(term, "Ağ Stol");  // söz "Stol" tam söz → 1
        var contains = SearchNormalizer.Score(term, "Barstool");   // "stol" yoxdur; "stool"≠ → yoxla

        Assert.Equal(0, exact);
        Assert.True(wordPrefix < SearchNormalizer.NoMatch);
        Assert.True(exact < wordPrefix);
        // "Barstool" normalizə → "barstool", "stol" daxilində yoxdur → NoMatch
        Assert.Equal(SearchNormalizer.NoMatch, contains);
    }

    [Fact]
    public void Whole_word_ranks_above_inside_word()
    {
        var term = SearchNormalizer.Normalize("stol");
        var wholeWord = SearchNormalizer.Score(term, "Ağ Stol");     // tam söz → 1
        var insideWord = SearchNormalizer.Score(term, "Stolüstü");   // sözün əvvəli → 2
        Assert.True(wholeWord < insideWord);
    }
}
