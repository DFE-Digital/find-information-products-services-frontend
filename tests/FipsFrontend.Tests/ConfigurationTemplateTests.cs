using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using FipsFrontend.Models;

namespace FipsFrontend.Tests;

/// <summary>
/// The settings template is what a new instance is configured from. A key the code reads that the
/// template does not name is a setting nobody configuring the service can know exists.
/// </summary>
[TestFixture]
public class ConfigurationTemplateTests
{
    // The ways this code reads configuration by literal key: indexer, GetValue<T>, GetSection, GetConnectionString.
    private static readonly Regex LiteralRead = new(
        @"(?:_configuration|Configuration|configuration|builder\.Configuration|config)\s*(?:\[|\.GetValue<[^>]+>\(|\.GetSection\(|\.GetConnectionString\()\s*""([A-Za-z0-9:_-]+)""",
        RegexOptions.Compiled);

    // Sections the framework reads; the template need not name them for the application's sake.
    private static readonly string[] FrameworkSections = ["Logging", "AllowedHosts"];

    [Test]
    public void ConfigurationTemplate_NamesEveryKeyTheCodeReadsByName()
    {
        var keysRead = ApplicationSourceFiles()
            .SelectMany(f => LiteralRead.Matches(File.ReadAllText(f)).Select(m => m.Groups[1].Value))
            .Where(k => !FrameworkSections.Contains(k.Split(':')[0]))
            .Distinct()
            .Order()
            .ToList();
        Assert.That(keysRead, Is.Not.Empty, "no configuration reads found: the pattern no longer matches the code");

        var missing = keysRead.Where(k => !TemplateKeys().Contains(k)).ToList();

        Assert.That(missing, Is.Empty, "appsettings.template.json does not name these keys the code reads:\n  " + string.Join("\n  ", missing));
    }

    [Test]
    public void ConfigurationTemplate_NamesEverySettingAnOptionsClassBinds()
    {
        var expected = typeof(FeedbackOptions).Assembly.GetTypes()
            .Select(t => (Type: t, Section: t.GetField("SectionName", BindingFlags.Public | BindingFlags.Static)?.GetRawConstantValue() as string))
            .Where(x => x.Section is not null)
            .SelectMany(x => x.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.GetCustomAttribute<ObsoleteAttribute>() is null)
                .Select(p => $"{x.Section}:{p.Name}"))
            .Order()
            .ToList();
        Assert.That(expected, Is.Not.Empty, "no options classes with a SectionName constant were found");

        var missing = expected.Where(k => !TemplateKeys().Contains(k)).ToList();

        Assert.That(missing, Is.Empty, "appsettings.template.json does not name these settings an options class binds:\n  " + string.Join("\n  ", missing));
    }

    private static IEnumerable<string> ApplicationSourceFiles() =>
        Directory.EnumerateFiles(ApplicationDirectory(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    private static HashSet<string> TemplateKeys()
    {
        using var template = JsonDocument.Parse(File.ReadAllText(Path.Combine(ApplicationDirectory(), "appsettings.template.json")));
        return new HashSet<string>(Flatten(template.RootElement, ""));
    }

    private static IEnumerable<string> Flatten(JsonElement element, string prefix)
    {
        if (element.ValueKind != JsonValueKind.Object) yield break;
        foreach (var property in element.EnumerateObject())
        {
            var key = prefix.Length == 0 ? property.Name : $"{prefix}:{property.Name}";
            yield return key;
            foreach (var child in Flatten(property.Value, key)) yield return child;
        }
    }

    /// <summary>The web project's directory: under the repository root, which is the directory holding the solution file.</summary>
    private static string ApplicationDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FipsFrontend.slnx")))
        {
            dir = dir.Parent;
        }
        var root = dir?.FullName ?? throw new InvalidOperationException("FipsFrontend.slnx not found above the test assembly");
        return Path.Combine(root, "src", "FipsFrontend");
    }
}
