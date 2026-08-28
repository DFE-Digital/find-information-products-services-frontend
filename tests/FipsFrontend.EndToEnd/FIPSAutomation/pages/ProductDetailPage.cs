using FiPSAutomation.Components;
using Microsoft.Playwright;

namespace FiPSAutomation.Pages
{
    public class ProductDetailPage
    {
        private readonly IPage page;

        // Product link
        private ILocator ProductLink => page.Locator("//a[contains(text(), \"Accessibility and inclusion manual\")]");

        // Table columns
        private ILocator PhaseColumn => page.Locator("//th[normalize-space()='Phase']");
        private ILocator BusinessAreaColumn => page.Locator("//th[normalize-space()='Business area']");
        private ILocator ContactsColumn => page.Locator("//th[normalize-space()='Contacts']");
        private ILocator ViewProductColumn => page.Locator("//th[normalize-space()='View product']");

        // Side navigation
        private ILocator OverviewLink => page.Locator("//*[@id=\"side-navigation\"]/li[1]/a");
        private ILocator CategoriesLink => page.Locator("//*[@id=\"side-navigation\"]/li[2]/a");
        private ILocator ProposeChangeLink => page.Locator("//*[@id=\"side-navigation\"]/li[3]/a");

        // Contacts
        private ILocator ResponsibilitiesAndContactsHeader => page.Locator("#contacts");
        private ILocator ContactsNameLink => page.Locator("//a[text()='Andy JONES']");
        private ILocator ServiceOwnerLocator => page.Locator("(//dt[@class = \"govuk-summary-list__key\"])[1]");

        // Tables
        public string CategoriesTable => "//main[@id='main-content']//table[1]";
        public string UsersOfProductTable => "//main[@id='main-content']//table[2]";
        public string ProductDetailTable => "(//table[@class = \"govuk-table\"])[1]";
        public string CategoriesDetailTable => "(//table[@class = \"govuk-table\"])[2]";
        public string UserProductTable => "(//table[@class = \"govuk-table\"])[3]";
        public string tableHeader => ".govuk-table__head .govuk-table__row";
        public string tableRow => ".govuk-table__body .govuk-table__row";

        // Beta banner
      //  private ILocator BetaPhaseBanner => page.Locator("div[class='govuk-phase-banner'] div[class='govuk-width-container']");

        // Feedback banner
        private ILocator FeedbackBanner => page.Locator("//div[@class='dfe-feedback-banner--flex']");
        private ILocator SurveyLinkText => page.Locator("//*[@id=\"feedback-link-text\"]/a[1]");

        // Users of Product 
        private ILocator SearchTermsList => page.Locator(".govuk-details__summary-text");

        //Page URL

        public static readonly string GOV_URL = "https://www.gov.uk/government/organisations/department-for-education/about/personal-information-charter";
        public static readonly string Product_URL = "https://accessibility.education.gov.uk";
        public static readonly string Accessibility_URL = "https://accessibility.education.gov.uk/";
        public static readonly string Accessibility_URL_Test = Accessibility_URL + "/automation-test";
        public ProductDetailPage(IPage page)
        {
            this.page = page;
        }

        public async Task ClickProductLinkAsync()
        {
            await ProductLink.ClickAsync();
        }

        public async Task VerifyOverviewHeadersAsync()
        {
            await Assertions.Expect(PhaseColumn).ToBeVisibleAsync();
            await Assertions.Expect(BusinessAreaColumn).ToBeVisibleAsync();
            await Assertions.Expect(ContactsColumn).ToBeVisibleAsync();
            await Assertions.Expect(ViewProductColumn).ToBeVisibleAsync();
        }

        public async Task ClickOverviewLinkAsync()
        {
            await OverviewLink.ClickAsync();
        }

        public async Task ClickCategoriesLinkAsync()
        {
            await CategoriesLink.ClickAsync();
        }

        public async Task ClickProposeChangeLinkAsync()
        {
            await ProposeChangeLink.ClickAsync();
        }

        public async Task VerifyResponsibilitiesHeaderAsync()
        {
            await Assertions.Expect(ResponsibilitiesAndContactsHeader).ToBeVisibleAsync();
        }

        public async Task VerifyContactsNameLinkAsync()
        {
            await Assertions.Expect(ContactsNameLink).ToBeVisibleAsync();
        }

        public async Task VerifyServiceOwnerAsync()
        {
            await Assertions.Expect(ServiceOwnerLocator).ToBeVisibleAsync();
        }

        //public async Task VerifyBetaPhaseBannerAsync()
        //{
        //    await Assertions.Expect(BetaPhaseBanner).ToBeVisibleAsync();
        //}

        public async Task VerifyFeedbackBannerAsync()
        {
            await Assertions.Expect(FeedbackBanner).ToBeVisibleAsync();
        }

        public async Task ClickSurveyLinkAsync()
        {
            await SurveyLinkText.ClickAsync();
        }

        public async Task VerifySurveyPageAsync()
        {
            await Assertions.Expect(page.Locator("//div[@class='QuestionText BorderColor']")).ToBeVisibleAsync();
        }

        /*public async Task AssertProductTableAsync(List<Dictionary<string, string>> expectedRows)
         {
             await GovTableComponent.AssertTableColumnValuesAsync(page, expectedRows, ProductDetailTable);
         }*/

        public async Task AssertCategoriesTableAsync(List<Dictionary<string, string>> expectedRows)
        {
            await GovTableComponent.AssertTableColumnValuesAsync(page, expectedRows, CategoriesDetailTable);
        }

        /*public async Task AssertUsersTableAsync(List<Dictionary<string, string>> expectedRows)
        {
            await GovTableComponent.AssertTableColumnValuesAsync(page, expectedRows, UserProductTable);
        }*/

        public async Task VerifySearchTermsListAsync(string expectedText)
        {
            await Assertions.Expect(SearchTermsList).ToContainTextAsync(expectedText);
            await SearchTermsList.ClickAsync();
        }
    }
}
