using TimesheetAssistant.Core.Features.Workdays;

namespace TimesheetAssistant.UnitTests.Features.Workdays;

public class WorkEntryValidatorTests
{
    private static readonly DateOnly Today = new(2026, 11, 12);

    [Theory]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    [InlineData("  details  ", "details")]
    public void Trims_fields_without_imposing_a_ticket_pattern(string? description, string expectedDescription)
    {
        // Arrange
        var input = new WorkEntryInput("  arbitrary ticket  ", "  A title  ", description, "1h");
        // Act
        var result = WorkEntryValidator.Validate(Today, Today, input);
        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(new WorkEntryValues("arbitrary ticket", "A title", expectedDescription, 60), result.Values);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null, "title", "JiraId")]
    [InlineData("  ", "title", "JiraId")]
    [InlineData("T-1", null, "Title")]
    [InlineData("T-1", "  ", "Title")]
    public void Rejects_missing_required_text(string? jiraId, string? title, string field)
    {
        // Arrange
        var input = new WorkEntryInput(jiraId, title, null, "1m");
        // Act
        var result = WorkEntryValidator.Validate(Today, Today, input);
        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(field, result.Errors.Keys);
    }

    [Fact]
    public void Does_not_add_field_length_limits()
    {
        // Arrange
        var text = new string('x', 10000);
        var input = new WorkEntryInput(text, text, text, "1m");
        // Act
        var result = WorkEntryValidator.Validate(Today, Today, input);
        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(text, result.Values!.Description);
    }
}
