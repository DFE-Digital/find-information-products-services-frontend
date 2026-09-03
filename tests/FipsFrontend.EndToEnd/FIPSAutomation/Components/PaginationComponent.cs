using Microsoft.Playwright;

namespace FiPSAutomation.Components
{
    public class PaginationComponent
    {
        private readonly IPage page;

        private ILocator NextPageLink => page.Locator("//span[@class ='govuk-pagination__link-title' and contains(text(), 'Next')]");

        public PaginationComponent(IPage page)
        {
            this.page = page;
        }

        public async Task GoToPageAsync(int pageNumber)
        {
            await page.Locator($"a[aria-label = \"Page {pageNumber}\"]").ClickAsync();
        }

        public async Task GoToNextPageAsync()
        {
            await NextPageLink.ClickAsync();
        }

        /// <summary>
        /// Asserts the page's path and query, ignoring which host the run is against.
        /// </summary>
        /// <remarks>
        /// The origin is not what a pagination assertion is about: it checks that a click produced the right
        /// route and parameters. Asserting the host made these true in exactly one hosted environment and false
        /// everywhere else, and reported the whole url as wrong when only the origin differed.
        /// Uri parses the url rather than a pattern matching it: an escaped-suffix pattern also matches a LONGER
        /// path ending in the same characters, so /Admin/Products?page=2 would satisfy /Products?page=2,
        /// a false pass, which is the direction an assertion must never fail in.
        /// Query parameters compare as a set of pairs because their order is not something the page promises;
        /// one assertion expected type=api&amp;type=information and the page emitted the reverse.
        /// A predicate, retried by WaitForURLAsync, because the url changes after a click and a single read races it.
        /// </remarks>
        public async Task VerifyPathAndQueryAsync(string pathAndQuery)
        {
            var expected = "/" + pathAndQuery.TrimStart('/');
            try
            {
                await page.WaitForURLAsync(url => Urls.SamePathAndQuery(url, expected));
            }
            catch (TimeoutException)
            {
                // Playwright's own timeout names neither url; both are stated, reduced to the compared part, query sorted.
                throw new PlaywrightException(
                    $"Expected path and query:\n  {Urls.Describe(Urls.Absolute(expected))}\nbut the page is at:\n  {Urls.Describe(new Uri(page.Url))}\n\n"
                    + "Query parameters are compared as a set, so their order is not the difference. "
                    + "The origin is not asserted: this checks the route and parameters a click produced, so it passes against any environment.");
            }
        }

    }
}
