using Microsoft.Extensions.Configuration;

namespace FipsFrontend.Configuration;

/// <summary>
/// The request limiter: a fixed window per partition, beyond which a caller is refused with 429 and a Retry-After.
/// Both values default, so an instance with nothing configured admits the defaults below; an instance driven by an
/// automated suite raises the limit, because a suite at seconds-scale waits exhausts 100 permits in well under a
/// minute and every page after that is a refusal that names the page rather than the limiter.
/// How a partition is keyed is stated where the limiter is registered in <c>Program.cs</c>. Rules in <see cref="ConfigurationSections"/>.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string Section = "RateLimiting";

    // The defaults, stated once.
    public const int DefaultPermitLimitPerWindow = 100;
    public const int DefaultWindowSeconds = 60;

    /// <summary>Requests admitted per partition per window.</summary>
    public int PermitLimitPerWindow { get; private init; } = DefaultPermitLimitPerWindow;

    /// <summary>Length of the fixed window, in seconds.</summary>
    public int WindowSeconds { get; private init; } = DefaultWindowSeconds;

    public TimeSpan Window => TimeSpan.FromSeconds(WindowSeconds);

    public static RateLimitingOptions Read(IConfiguration configuration)
    {
        var section = configuration.GetSection(Section);
        return new RateLimitingOptions
        {
            PermitLimitPerWindow = PositiveOrDefault(section, nameof(PermitLimitPerWindow), DefaultPermitLimitPerWindow),
            WindowSeconds = PositiveOrDefault(section, nameof(WindowSeconds), DefaultWindowSeconds),
        };
    }

    // Rule 2 applied to a number: empty is absent, so the default; anything else must be a positive whole number.
    private static int PositiveOrDefault(IConfigurationSection section, string key, int fallback)
    {
        var raw = section[key];
        if (ConfigurationSections.IsAbsent(raw)) return fallback;
        if (int.TryParse(raw, out var value) && value > 0) return value;
        throw new InvalidOperationException($"{Section}:{key} must be a positive whole number; found '{raw}'.");
    }
}
