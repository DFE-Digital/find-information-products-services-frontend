using AventStack.ExtentReports;
using Deque.AxeCore.Playwright;

namespace FiPSAutomation;

[TestFixture, Order(21)]
[Category("Accessibility")]
public class AccessibilityTests : BaseTest
{
    [Test, Order(131), Category("accessibility")]
    public async Task AccessibilityTest()
    {
        var axeResults = await Page.RunAxe();
        var violations = axeResults.Violations;

        foreach (var violation in axeResults.Violations)
        {
            ExtentTest?.Info($"Impact: {violation.Impact}");
            ExtentTest?.Info($"Description: {violation.Description}");

            foreach (var node in violation.Nodes)
            {
                ExtentTest?.Info($"Element: {node.Html}");

                // Log any failure messages from the 'Any' checks
                foreach (var check in node.Any)
                {
                    ExtentTest?.Info($"Check (Any): {check.Message}");
                }

                // Log all required checks that failed
                foreach (var check in node.All)
                {
                    ExtentTest?.Info($"Check (All): {check.Message}");
                }
            }
        }
    }
}
