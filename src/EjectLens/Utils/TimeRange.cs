namespace EjectLens.Utils;

/// <summary>
/// Predefined time ranges for event log queries.
/// </summary>
public enum TimeRange
{
    Minutes15,
    Hour1,
    Hours2,
    Hours24
}

public static class TimeRangeExtensions
{
    public static TimeSpan ToTimeSpan(this TimeRange range) => range switch
    {
        TimeRange.Minutes15 => TimeSpan.FromMinutes(15),
        TimeRange.Hour1 => TimeSpan.FromHours(1),
        TimeRange.Hours2 => TimeSpan.FromHours(2),
        TimeRange.Hours24 => TimeSpan.FromHours(24),
        _ => TimeSpan.FromHours(2)
    };

    public static string ToDisplayString(this TimeRange range) => range switch
    {
        TimeRange.Minutes15 => "Last 15 minutes",
        TimeRange.Hour1 => "Last 1 hour",
        TimeRange.Hours2 => "Last 2 hours",
        TimeRange.Hours24 => "Last 24 hours",
        _ => "Last 2 hours"
    };

    public static string ToReportString(this TimeRange range) => range switch
    {
        TimeRange.Minutes15 => "15 minutes",
        TimeRange.Hour1 => "1 hour",
        TimeRange.Hours2 => "2 hours",
        TimeRange.Hours24 => "24 hours",
        _ => "2 hours"
    };
}
