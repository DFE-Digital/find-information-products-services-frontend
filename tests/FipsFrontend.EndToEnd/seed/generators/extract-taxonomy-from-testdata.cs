#:package ClosedXML
#:property PublishAot=false
// Regenerates scenarios/taxonomy-from-testdata-xlsx.generated.json from the suite's own data sheet, testdata.xlsx:
// the categories and the three-level user groups as the tests expect them. The sheet is the only record of that
// taxonomy, so the fixture is derived from it rather than written by hand.
//
//   dotnet run generators/extract-taxonomy-from-testdata.cs -- [--xlsx ../testdata.xlsx] [--out scenarios/taxonomy-from-testdata-xlsx.generated.json]
//
// Sheets read: category_* (a Product_Locator url per value, and a filter-badge selector naming its label);
// usergroup_*_list (a page url naming the parent, then the sub-category labels shown on that page); UG_EPEY*
// (the second level's own urls); UGSubcategory_*, UGSubcateg_*, EPEYSubcateg* and EPEYSubcatg* (the third level,
// one badge selector per value under the sheet's subject).
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

string Option(string name, string fallback)
{
    var i = Array.IndexOf(args, "--" + name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
}

var xlsx = Path.GetFullPath(Option("xlsx", Path.Combine("..", "testdata.xlsx")));
var outPath = Path.GetFullPath(Option("out", Path.Combine("scenarios", "taxonomy-from-testdata-xlsx.generated.json")));
if (!File.Exists(xlsx)) { Console.Error.WriteLine($"not found: {xlsx}"); return 2; }

// The badge cell is a CSS selector literal of the form a.filter-badge:has(span:text-is('<label>')); the label is
// the quoted text inside text-is(...).
var badgeLabel = new Regex(@"text-is\('([^']+)'\)");
string? LabelIn(IEnumerable<string> cells) => cells.Select(c => badgeLabel.Match(c)).FirstOrDefault(m => m.Success)?.Groups[1].Value;

using var workbook = new XLWorkbook(xlsx);
var sheets = workbook.Worksheets.Select(w => w.Name).ToList();
List<List<string>> Rows(string sheet) => workbook.Worksheet(sheet).RowsUsed()
    .Select(r => r.CellsUsed().OrderBy(c => c.Address.ColumnNumber).Select(c => c.GetString()).ToList())
    .ToList();
List<string> NonEmpty(List<string> row) => row.Where(c => !string.IsNullOrEmpty(c)).ToList();

var categories = new Dictionary<string, List<object>>();
foreach (var sheet in sheets.Where(s => s.StartsWith("category_", StringComparison.Ordinal)))
{
    var kind = sheet["category_".Length..];
    var data = Rows(sheet);
    if (data.Count == 0) continue;
    var header = data[0];
    var locator = Math.Max(header.IndexOf("Product_Locator"), 0);
    var entries = new List<object>();
    foreach (var row in data.Skip(1))
    {
        if (row.Count <= locator || string.IsNullOrEmpty(row[locator])) continue;
        var url = row[locator];
        var slug = url.Contains('=') ? url[(url.LastIndexOf('=') + 1)..] : url.TrimEnd('/')[(url.TrimEnd('/').LastIndexOf('/') + 1)..];
        entries.Add(new { slug, label = LabelIn(row), url });
    }
    categories[kind] = entries;
}

var userGroupValues = new List<object>();
var userGroupChildren = new Dictionary<string, List<string>>();
foreach (var sheet in sheets.Where(s => s.StartsWith("usergroup_", StringComparison.Ordinal) && s.EndsWith("_list", StringComparison.Ordinal)))
{
    string? parentUrl = null;
    var labels = new List<string>();
    foreach (var row in Rows(sheet))
    {
        var cells = NonEmpty(row);
        if (cells.Count == 0) continue;
        if (parentUrl is null && cells[0].StartsWith("categories/user-group/", StringComparison.Ordinal)) { parentUrl = cells[0]; continue; }
        if (cells.Count == 1 && !cells[0].StartsWith("//", StringComparison.Ordinal) && !cells[0].StartsWith("categories", StringComparison.Ordinal)) labels.Add(cells[0]);
    }
    if (parentUrl is null) continue;
    var segments = parentUrl.TrimEnd('/').Split('/');
    var slug = segments[^1];
    var parentLabel = labels.Count > 0 ? labels[0] : slug;
    // A sheet whose url is more than one level below user-group describes a value that is itself a child; its
    // parent's sheet lists it, so it is not claimed as top level here, only its own children are registered.
    var depth = Array.IndexOf(segments, "user-group") is var at && at >= 0 ? segments.Length - at - 1 : 1;
    if (depth <= 1) userGroupValues.Add(new { slug, label = parentLabel, url = parentUrl });
    var kids = labels.Skip(1).Where(l => !string.Equals(l, "sub-categories", StringComparison.OrdinalIgnoreCase)).ToList();
    if (kids.Count > 0) userGroupChildren[slug] = kids;
}

var secondLevelUrls = new List<string>();
foreach (var sheet in sheets.Where(s => s.StartsWith("UG_EPEY", StringComparison.Ordinal)))
{
    var first = Rows(sheet).Select(NonEmpty).FirstOrDefault(cells => cells.Count > 0 && cells[0].StartsWith("categories/user-group/", StringComparison.Ordinal));
    if (first is not null) secondLevelUrls.Add(first[0]);
}

// The third level is reached by searching for a value's name rather than by a route, so it leaves no trace in any
// route table; these sheets are the only record of it.
var thirdLevel = new Dictionary<string, List<string>>();
foreach (var sheet in sheets.Where(s => s.StartsWith("UGSubcategory_", StringComparison.Ordinal) || s.StartsWith("UGSubcateg_", StringComparison.Ordinal)
                                     || s.StartsWith("EPEYSubcateg", StringComparison.Ordinal) || s.StartsWith("EPEYSubcatg", StringComparison.Ordinal)))
{
    var names = new List<string>();
    foreach (var row in Rows(sheet))
        foreach (var cell in row)
        {
            var m = badgeLabel.Match(cell);
            if (m.Success && !names.Contains(m.Groups[1].Value)) names.Add(m.Groups[1].Value);
        }
    if (names.Count > 0) thirdLevel[sheet] = names;
}

var result = new Dictionary<string, object>
{
    ["_about"] = "The taxonomy as the browser suite's data sheet (testdata.xlsx) expects it, including the deeper user-group values. Generated by generators/extract-taxonomy-from-testdata.cs; do not hand-edit, regenerate with npm run generate:taxonomy.",
    ["categories"] = categories,
    ["userGroups"] = new { values = userGroupValues, children = userGroupChildren },
    ["sheets"] = sheets,
    ["secondLevelUrls"] = secondLevelUrls,
    ["thirdLevel"] = thirdLevel,
};

if (Path.GetDirectoryName(outPath) is { Length: > 0 } outDirectory) Directory.CreateDirectory(outDirectory);
// LF regardless of platform, so the committed output is byte-identical whichever machine regenerates it.
var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true, NewLine = "\n", Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
File.WriteAllText(outPath, json + "\n");
Console.WriteLine($"wrote {outPath}");
foreach (var (kind, entries) in categories) Console.WriteLine($"  category {kind,-14} {entries.Count} values");
Console.WriteLine($"  user groups     {userGroupValues.Count} top level, {userGroupChildren.Count} with children");
Console.WriteLine($"  third level     {thirdLevel.Values.Sum(v => v.Count)} values across {thirdLevel.Count} sheets");
return 0;
