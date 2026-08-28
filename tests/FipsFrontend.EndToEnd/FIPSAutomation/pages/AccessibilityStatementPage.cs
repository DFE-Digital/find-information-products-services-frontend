using Microsoft.Playwright;

namespace FiPSAutomation.Pages
{
    public class AccessibilityStatementPage
    {
        private readonly IPage page;

        public AccessibilityStatementPage(IPage page)
        {
            this.page = page;
        }

        public async Task VerifyAccessibilityStatementVisibleAsync()
        {
            await Assertions.Expect(page.GetByText("Accessibility statement", new() { Exact = true })).ToBeVisibleAsync();
        }
    }
}
