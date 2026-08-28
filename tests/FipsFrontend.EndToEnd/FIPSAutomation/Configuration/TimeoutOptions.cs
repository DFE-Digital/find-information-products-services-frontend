namespace FiPSAutomation.Configuration;

/// <summary>
/// How long the suite waits before an assertion, an action, or a navigation fails. OPTIONAL: every
/// value has Playwright's default. Set them low for an application on the same machine: a failing
/// assertion waits the whole expect timeout and a page that never appears waits the whole
/// navigation timeout, so against a loopback target the defaults make a run mostly waiting
/// (measured: 22 of the first 35 failures took exactly 5 s and 7 took exactly 30 s).
/// </summary>
public sealed class TimeoutOptions
{
    public const string Section = "Timeouts";

    /// <summary>Wait for an <c>Expect(...)</c> assertion to hold. Playwright's default is 5000.</summary>
    public int ExpectMs { get; set; } = 5_000;

    /// <summary>Wait for an action (click, fill) and its element to become actionable. Playwright's default is 30000.</summary>
    public int ActionMs { get; set; } = 30_000;

    /// <summary>Wait for a navigation to commit. Playwright's default is 30000.</summary>
    public int NavigationMs { get; set; } = 30_000;

    public IEnumerable<string> NonPositive()
    {
        if (ExpectMs <= 0) yield return nameof(ExpectMs);
        if (ActionMs <= 0) yield return nameof(ActionMs);
        if (NavigationMs <= 0) yield return nameof(NavigationMs);
    }
}
