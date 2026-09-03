// Summarises a browser-suite trx: outcomes, failures grouped by cause, and what the known-green ratchet would say.
// A file-based app, so it needs no project: run it from the repository root with
//
//   dotnet run tests/FipsFrontend.EndToEnd/tools/summarise-trx.cs -- <path.trx>
//
// Check-KnownGreen.ps1 is the gate; this is the reading alongside it, for a person deciding what a run means.
// Excluded from the test project's compilation by the csproj (tools/** is not a test).
using System.Text.RegularExpressions;
using System.Xml.Linq;

if (args.Length < 1) { Console.Error.WriteLine("usage: summarise-trx.cs <path.trx>"); return 2; }
var trx = XDocument.Load(args[0]);
XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
var here = Path.GetDirectoryName(Path.GetFullPath(Environment.GetCommandLineArgs()[0])) ?? ".";
var suiteDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
string ListPath(string name) => new[] { Path.Combine(Directory.GetCurrentDirectory(), "tests", "FipsFrontend.EndToEnd", name), Path.Combine(Directory.GetCurrentDirectory(), name) }
    .FirstOrDefault(File.Exists) ?? name;
HashSet<string> Names(string path) => File.Exists(path)
    ? File.ReadLines(path).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith('#')).ToHashSet()
    : new HashSet<string>();

var results = trx.Descendants(ns + "UnitTestResult")
    .Select(r => (Name: r.Attribute("testName")?.Value ?? "", Outcome: r.Attribute("outcome")?.Value ?? "", Message: r.Element(ns + "Output")?.Element(ns + "ErrorInfo")?.Element(ns + "Message")?.Value ?? ""))
    .ToList();

var outcomes = results.GroupBy(r => r.Outcome).ToDictionary(g => g.Key, g => g.Count());
Console.WriteLine($"outcomes: {string.Join(", ", outcomes.Select(kv => $"{kv.Key} {kv.Value}"))}; total {results.Count}");

static string Cause(string text)
{
    var first = text.Trim().Split('\n').FirstOrDefault()?.Trim() ?? "";
    if (text.Contains("429") || text.Contains("Too many requests") || text.Contains("503") || text.Contains("Service Unavailable")) return "rate limit (429) or unavailable (503)";
    if (text.Contains("strict mode violation")) return "strict mode (locator matched several)";
    if (first.Contains("Timeout") && first.Contains("ms")) return "timeout waiting for " + (text.Contains("Locator") || text.Contains("locator") ? "locator" : "action");
    if (text.Contains("expected to be visible")) return "expected to be visible";
    if (text.Contains("expected to have text")) return "expected to have text";
    if (text.Contains("ERR_CONNECTION") || text.Contains("net::")) return "connection error";
    return Regex.Replace(first, @"\d+", "N") is { Length: > 0 } f ? (f.Length > 90 ? f[..90] : f) : "no message";
}

var failed = results.Where(r => r.Outcome == "Failed").ToList();
Console.WriteLine("\nfailures by cause:");
foreach (var g in failed.GroupBy(r => Cause(r.Message)).OrderByDescending(g => g.Count()).Take(15))
    Console.WriteLine($"  {g.Count(),3}  {g.Key}");

var green = Names(ListPath("known-green.txt"));
var flaky = Names(ListPath("known-flaky.txt"));
var byName = results.ToDictionary(r => r.Name, r => r.Outcome);
var regressed = green.Where(n => byName.TryGetValue(n, out var o) && o != "Passed").OrderBy(n => n).ToList();
var newlyGreen = results.Where(r => r.Outcome == "Passed" && !green.Contains(r.Name) && !flaky.Contains(r.Name)).Select(r => r.Name).OrderBy(n => n).ToList();
Console.WriteLine($"\nknown-green: {green.Count} listed, regressed {regressed.Count}{(regressed.Count > 0 ? ": " + string.Join(", ", regressed) : "")}");
Console.WriteLine($"newly green, not yet listed: {newlyGreen.Count}");
foreach (var n in newlyGreen) Console.WriteLine($"  + {n}");
// --show <name>: the full assertion message of one result, for reading what a locator matched.
var showAt = Array.IndexOf(args, "--show");
if (showAt >= 0 && showAt + 1 < args.Length)
{
    var wanted = args[showAt + 1];
    foreach (var r in results.Where(r => r.Name.Contains(wanted, StringComparison.OrdinalIgnoreCase)))
        Console.WriteLine($"\n== {r.Name} [{r.Outcome}]\n{string.Join("\n", r.Message.Split('\n').Take(40))}");
    return 0;
}
// --app-log <path>: the application's console log; a test whose run overlaps a refusal the application answered is
// named, because the request limiter refuses in under a millisecond once a host exceeds RateLimiting:PermitLimitPerWindow
// (100 a minute by default), and the suite at its local pace does exactly that. Such a failure reads as "expected to
// be visible" and names nothing about rate limiting, so it is worth cross-referencing rather than chasing as a
// locator. The limiter answers 429; 503 is matched too, since a hosting layer or an older build answers that.
var logAt = Array.IndexOf(args, "--app-log");
if (logAt >= 0 && logAt + 1 < args.Length && File.Exists(args[logAt + 1]))
{
    // Shared read: the application is usually still running and holds its log open for writing.
    using var log = new StreamReader(new FileStream(args[logAt + 1], FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
    var lines = new List<string>();
    while (log.ReadLine() is { } line) lines.Add(line);
    var rejections = lines
        .Where(l => l.Contains("Status: 429") || l.Contains("Status: 503"))
        .Select(l => Regex.Match(l, @"Timestamp = (\d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2})"))
        .Where(m => m.Success)
        .Select(m => DateTime.ParseExact(m.Groups[1].Value, "dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture))
        .ToList();
    var windows = trx.Descendants(ns + "UnitTestResult")
        .Select(r => (Name: r.Attribute("testName")?.Value ?? "", Outcome: r.Attribute("outcome")?.Value ?? "",
            Start: DateTime.Parse(r.Attribute("startTime")?.Value ?? "", null, System.Globalization.DateTimeStyles.RoundtripKind).ToLocalTime(),
            End: DateTime.Parse(r.Attribute("endTime")?.Value ?? "", null, System.Globalization.DateTimeStyles.RoundtripKind).ToLocalTime()))
        .ToList();
    var hit = windows.Where(w => rejections.Any(t => t >= w.Start.AddSeconds(-1) && t <= w.End.AddSeconds(1))).ToList();
    Console.WriteLine($"\nrate limiter: {rejections.Count} rejection(s) in the application log; {hit.Count} test(s) ran while one was answered:");
    foreach (var w in hit) Console.WriteLine($"  {(w.Outcome == "Failed" ? "-" : "+")} {w.Name} [{w.Outcome}]");
}
var all = args.Contains("--all");
Console.WriteLine(all ? "\nfailing:" : "\nfailing, first twelve (--all for every one):");
foreach (var r in all ? failed : failed.Take(12)) Console.WriteLine($"  - {r.Name} :: {Cause(r.Message)}");
return 0;
