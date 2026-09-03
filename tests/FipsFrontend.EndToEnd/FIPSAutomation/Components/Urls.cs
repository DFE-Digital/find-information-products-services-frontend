using System.Web;

namespace FiPSAutomation.Components
{
    /// <summary>
    /// How the suite compares a url the browser reached with the path and query a test expects. No browser in it:
    /// the rules project holds its decisions in tests that run anywhere.
    /// </summary>
    public static class Urls
    {
        // A relative reference needs a base to be parsed at all; the base is discarded and never compared.
        public static Uri Absolute(string pathAndQuery) => new(new Uri("https://base.invalid"), pathAndQuery);

        /// <summary>
        /// Whether a url's path and query match an expected path and query, ignoring the origin and the order of
        /// query parameters.
        /// </summary>
        public static bool SamePathAndQuery(string actualUrl, string expectedPathAndQuery)
        {
            var actual = new Uri(actualUrl);
            var expected = Absolute(expectedPathAndQuery);
            // The path compares without regard to case: routing is case-insensitive and the application renders
            // its links in lower case, so /Products and /products are one route rendered two ways. Values keep their case.
            return string.Equals(actual.AbsolutePath, expected.AbsolutePath, StringComparison.OrdinalIgnoreCase)
                && QueryPairs(actual).SequenceEqual(QueryPairs(expected), StringComparer.Ordinal);
        }

        // HttpUtility parses the query rather than splitting it by hand: repeated keys, percent-encoding and
        // empty values all appear in these filter urls, and a split on '&' and '=' gets each subtly wrong.
        public static List<string> QueryPairs(Uri uri)
        {
            var parsed = HttpUtility.ParseQueryString(uri.Query);
            var pairs = new List<string>();
            foreach (var key in parsed.AllKeys)
            {
                foreach (var value in parsed.GetValues(key) ?? Array.Empty<string>())
                {
                    pairs.Add($"{key}={value}");
                }
            }
            pairs.Sort(StringComparer.Ordinal);
            return pairs;
        }

        /// <summary>A url reduced to the part the comparison reads, query sorted, for a failure message.</summary>
        public static string Describe(Uri uri) =>
            uri.AbsolutePath + (QueryPairs(uri).Count > 0 ? "?" + string.Join("&", QueryPairs(uri)) + "  (sorted)" : "");
    }
}
