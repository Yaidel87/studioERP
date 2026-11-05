namespace StudioERP.Api.Services;

public static class StudioClock
{
    private static readonly TimeZoneInfo CubaTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Cuba Standard Time" : "America/Havana");

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CubaTimeZone);

    public static DateOnly Today => DateOnly.FromDateTime(Now);

    public static DateTime ToUtc(DateTime localDateTime)
    {
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, CubaTimeZone);
    }
}
