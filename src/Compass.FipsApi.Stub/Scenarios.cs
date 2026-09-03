using Compass.FipsApi.Contracts;

namespace Compass.FipsApi.Stub;

/// <summary>Resolves a scenario and request path to a response. Shared with the in-process tests, which serve the same files.</summary>
public sealed class Scenarios(string root)
{
    public const string Seeded = "seeded";
    public const string Drift = "drift";
    public const string Empty = "empty";
    public const string Unavailable = "unavailable";

    public sealed record Response(int Status, string Body);

    public Response Answer(string scenario, string path)
    {
        path = path.Trim('/');
        if (scenario == Unavailable) return new(503, """{"error":"unavailable by scenario"}""");
        if (scenario == Empty)
        {
            var (status, body) = EmptyAnswers.For(path);
            return new(status, body);
        }

        var file = FileFor(scenario, path);
        return file is null
            ? new(404, $$"""{"error":"no file answers this path in scenario '{{scenario}}': expected {{Path.Combine(scenario, path + ".json").Replace('\\', '/')}}"}""")
            : new(200, File.ReadAllText(file));
    }

    /// <summary>
    /// The file that answers a path: an exact match, else a product id's fallback <c>products/_by-id.json</c>.
    /// Only files under the scenario's own folder are ever named: the scenario and path come straight from the URL,
    /// and a path that climbs out of the folder (a <c>..</c> segment, an absolute path) answers as no file at all,
    /// so the stub cannot be asked to read and serve an arbitrary file.
    /// </summary>
    public string? FileFor(string scenario, string path)
    {
        path = path.Trim('/');
        var folder = Path.GetFullPath(Path.Combine(root, scenario)) + Path.DirectorySeparatorChar;
        if (!folder.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.Ordinal)) return null;

        var exact = Within(folder, path + ".json");
        if (exact is not null && File.Exists(exact)) return exact;
        if (path.StartsWith("api/v1/ServiceRegister/products/", StringComparison.Ordinal) && Guid.TryParse(path.Split('/')[^1], out _))
        {
            var fallback = Within(folder, "api/v1/ServiceRegister/products/_by-id.json");
            if (fallback is not null && File.Exists(fallback)) return fallback;
        }
        return null;
    }

    private static string? Within(string folder, string relative)
    {
        var full = Path.GetFullPath(Path.Combine(folder, relative));
        return full.StartsWith(folder, StringComparison.Ordinal) ? full : null;
    }
}
