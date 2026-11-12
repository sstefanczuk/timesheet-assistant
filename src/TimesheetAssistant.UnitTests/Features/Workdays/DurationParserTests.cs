using TimesheetAssistant.Core.Features.Workdays;

namespace TimesheetAssistant.UnitTests.Features.Workdays;

public class DurationParserTests
{
    [Theory]
    [InlineData("1h", 60)]
    [InlineData("30m", 30)]
    [InlineData("6h 12m", 372)]
    [InlineData("6h12m", 372)]
    [InlineData(" 6H   12M \t", 372)]
    [InlineData("90m", 90)]
    [InlineData("0h 1m", 1)]
    [InlineData("1h 0m", 60)]
    [InlineData("0001m", 1)]
    [InlineData("23h59m", 1439)]
    [InlineData("24h", 1440)]
    [InlineData("24h 0m", 1440)]
    [InlineData("1440m", 1440)]
    public void Accepts_documented_grammar(string text, int expected)
    {
        // Arrange
        var input = text;
        // Act
        var accepted = DurationParser.TryParse(input, out var minutes);
        // Assert
        Assert.True(accepted);
        Assert.Equal(expected, minutes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("0m")]
    [InlineData("0h")]
    [InlineData("0h0m")]
    [InlineData("24h 1m")]
    [InlineData("1441m")]
    [InlineData("25h")]
    [InlineData("6h 60m")]
    [InlineData("0h90m")]
    [InlineData("+1h")]
    [InlineData("-1m")]
    [InlineData("1.5h")]
    [InlineData("1,5h")]
    [InlineData("60")]
    [InlineData("6:12")]
    [InlineData("1h1h")]
    [InlineData("1m1m")]
    [InlineData("1m1h")]
    [InlineData("1h 2m 3m")]
    [InlineData("1 h")]
    [InlineData("1h\t2m")]
    [InlineData("1h\n2m")]
    [InlineData("1hour")]
    [InlineData("text 1h")]
    [InlineData("1h text")]
    [InlineData("١m")]
    [InlineData("１h")]
    [InlineData("2147483647h")]
    [InlineData("2147483648m")]
    [InlineData("999999999999999999999999999999999999h")]
    public void Rejects_invalid_or_out_of_range_input(string? text)
    {
        // Arrange
        var input = text;
        // Act
        var accepted = DurationParser.TryParse(input, out var minutes);
        // Assert
        Assert.False(accepted);
        Assert.Equal(0, minutes);
    }

    [Theory]
    [InlineData(0, "0m")]
    [InlineData(1, "1m")]
    [InlineData(60, "1h")]
    [InlineData(372, "6h 12m")]
    [InlineData(1440, "24h")]
    [InlineData(2881, "48h 1m")]
    public void Formats_totals_without_zero_components(long minutes, string expected)
    {
        // Arrange
        var total = minutes;
        // Act
        var formatted = DurationParser.Format(total);
        // Assert
        Assert.Equal(expected, formatted);
    }
}
