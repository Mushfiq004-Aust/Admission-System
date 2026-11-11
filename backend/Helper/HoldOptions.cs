namespace api.Helper
{
    // Settings for reservations. Read once at start-up from appsettings.json ("Hold" section).
    // The old environment-variable names (HOLD_DURATION_SECONDS, EXPIRATION_SWEEP_SECONDS) still win if set.
    public sealed class HoldOptions
    {
        public const int DefaultDurationSeconds = 600;
        public const int DefaultSweepIntervalSeconds = 5;

        // How long a seat is held before it expires.
        public int DurationSeconds { get; init; } = DefaultDurationSeconds;

        // How often the background worker marks stale holds EXPIRED.
        public int SweepIntervalSeconds { get; init; } = DefaultSweepIntervalSeconds;

        public static HoldOptions From(IConfiguration config) => new()
        {
            DurationSeconds = ParsePositive(
                config["HOLD_DURATION_SECONDS"] ?? config["Hold:DurationSeconds"], DefaultDurationSeconds),
            SweepIntervalSeconds = ParsePositive(
                config["EXPIRATION_SWEEP_SECONDS"] ?? config["Hold:SweepIntervalSeconds"], DefaultSweepIntervalSeconds)
        };

        private static int ParsePositive(string? raw, int fallback) =>
            int.TryParse(raw, out var value) && value > 0 ? value : fallback;
    }
}
