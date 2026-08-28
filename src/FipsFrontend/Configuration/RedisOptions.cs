using Microsoft.Extensions.Configuration;

namespace FipsFrontend.Configuration;

/// <summary>
/// The distributed cache. OPTIONAL: with <see cref="Enabled"/> false, or no address, the cache is
/// in-memory. Enabled with no address is refused at start-up - otherwise the cache client parses
/// an empty string on every request and logs an error each time while the pages carry on.
/// Rules in <see cref="ConfigurationSections"/>.
/// </summary>
public sealed class RedisOptions
{
    public const string Section = "Caching:Redis";

    public bool Enabled { get; set; }
    public string ConnectionString { get; set; } = "";
    public string KeyPrefix { get; set; } = "fips:";

    /// <summary>True when a distributed cache is to be used: switched on and given an address.</summary>
    public bool IsOn => Enabled && !ConfigurationSections.IsAbsent(ConnectionString);

    public static RedisOptions Read(IConfiguration configuration)
    {
        var section = configuration.GetSection(Section);
        var options = new RedisOptions
        {
            // Rule 2 applied to a boolean: an empty value is absent, so off - not a parse error.
            Enabled = bool.TryParse(section[nameof(Enabled)], out var enabled) && enabled,
            ConnectionString = section[nameof(ConnectionString)] ?? "",
            KeyPrefix = ConfigurationSections.IsAbsent(section[nameof(KeyPrefix)]) ? "fips:" : section[nameof(KeyPrefix)]!,
        };
        if (options.Enabled && ConfigurationSections.IsAbsent(options.ConnectionString))
        {
            throw new InvalidOperationException(
                $"{Section}:{nameof(Enabled)} is true but {Section}:{nameof(ConnectionString)} is empty. " +
                $"Supply the address, or set {Section}:{nameof(Enabled)} to false for an in-memory cache.");
        }
        return options;
    }
}
