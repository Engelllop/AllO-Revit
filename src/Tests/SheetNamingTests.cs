using System.Globalization;
using AllO.Helpers;
using AllO.UI.Converters;
using Xunit;

namespace AllO.Tests;

public class SheetNamingTests
{
    [Theory]
    [InlineData("A-{n}", 5, "A-5")]
    [InlineData("A-{nn}", 5, "A-05")]
    [InlineData("A-{nnn}", 5, "A-005")]
    [InlineData("{nn}", 12, "12")]
    [InlineData("E-{nnn}-X", 7, "E-007-X")]
    [InlineData("plain", 3, "plain")]
    public void ExpandNumberPattern_expands_placeholders(string pattern, int n, string expected)
        => Assert.Equal(expected, SheetNaming.ExpandNumberPattern(pattern, n));

    [Fact]
    public void ExpandNumberPattern_empty_returns_number()
        => Assert.Equal("9", SheetNaming.ExpandNumberPattern("", 9));

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("has,comma", "\"has,comma\"")]
    [InlineData("has \"quote\"", "\"has \"\"quote\"\"\"")]
    [InlineData(null, "")]
    public void CsvEscape_quotes_when_needed(string? input, string expected)
        => Assert.Equal(expected, SheetNaming.CsvEscape(input));
}

public class TitleSuffixConverterTests
{
    private readonly TitleSuffixConverter _c = new();

    [Theory]
    [InlineData("AllO — One Filter", "One Filter")]
    [InlineData("AllO - Batch Sheet Renamer", "Batch Sheet Renamer")]
    [InlineData("No dash here", "No dash here")]
    public void Convert_returns_part_after_dash(string input, string expected)
        => Assert.Equal(expected, _c.Convert(input, typeof(string), null!, CultureInfo.InvariantCulture));

    [Fact]
    public void Convert_null_returns_empty()
        => Assert.Equal("", _c.Convert(null!, typeof(string), null!, CultureInfo.InvariantCulture));
}

public class InverseBoolConverterTests
{
    private readonly InverseBoolConverter _c = new();

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Convert_inverts(bool input, bool expected)
        => Assert.Equal(expected, _c.Convert(input, typeof(bool), null!, CultureInfo.InvariantCulture));
}
