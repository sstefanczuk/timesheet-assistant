using System.Globalization;
using System.Text.RegularExpressions;

namespace TimesheetAssistant.Core.Features.Workdays;

public static partial class DurationParser
{
    [GeneratedRegex(@"\A(?:(?<hours>[0-9]+)[hH](?: *(?<minutes>[0-9]+)[mM])?|(?<minutes>[0-9]+)[mM])\z", RegexOptions.CultureInvariant)]
    private static partial Regex Grammar();

    public static bool TryParse(string? input, out int minutes)
    {
        minutes = 0;
        var match = Grammar().Match(input?.Trim() ?? string.Empty);
        if (!match.Success) return false;

        var hoursPart = match.Groups["hours"];
        var minutesPart = match.Groups["minutes"];
        if (hoursPart.Success && !int.TryParse(hoursPart.Value, NumberStyles.None, CultureInfo.InvariantCulture, out _)) return false;
        if (minutesPart.Success && !int.TryParse(minutesPart.Value, NumberStyles.None, CultureInfo.InvariantCulture, out _)) return false;
        var hours = hoursPart.Success ? int.Parse(hoursPart.Value, CultureInfo.InvariantCulture) : 0;
        var remainder = minutesPart.Success ? int.Parse(minutesPart.Value, CultureInfo.InvariantCulture) : 0;
        if (hoursPart.Success && minutesPart.Success && remainder > 59) return false;
        var total = (long)hours * 60 + remainder;
        if (total is < 1 or > 1440) return false;
        minutes = (int)total;
        return true;
    }

    public static string Format(long minutes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minutes);
        var hours = minutes / 60;
        var remainder = minutes % 60;
        return hours == 0 ? $"{remainder}m" : remainder == 0 ? $"{hours}h" : $"{hours}h {remainder}m";
    }
}
