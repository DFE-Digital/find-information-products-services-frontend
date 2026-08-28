using Microsoft.Playwright;

namespace FiPSAutomation.Components
{
    public class FilterTagsComponent
    {
        private readonly IPage page;

        private ILocator AppliedFiltersPanel => page.Locator("div.applied-filters-panel");
        //private ILocator ShowingResultsMessage => page.Locator("//p[@class =\"govuk-body\" and contains(text(), \"Showing \")]");

        // Business Area filter tags
        public string BA_Commercial => "a.filter-badge:has(span:text-is('Commercial'))";
        public string BA_CustomerExpAndDesign => "a.filter-badge:has(span:text-is('Customer Experience and Design'))";
        public string BA_ChildrenAndFamilies => "a.filter-badge:has(span:text-is('Children and Families'))";
        public string BA_EnterpriseData => "a.filter-badge:has(span:text-is('Enterprise Data'))";
        public string BA_FundingAndFinancialOversight => "a.filter-badge:has(span:text-is('Funding and Financial Oversight'))";
        public string BA_OperationsAndInfra => "a.filter-badge:has(span:text-is('Operations and Infrastructure'))";
        public string BA_RegionalDigitalServices => "a.filter-badge:has(span:text-is('Regional Digital Services'))";
        public string BA_SchoolsDigital => "a.filter-badge:has(span:text-is('Schools Digital'))";
        public string BA_SectorFacingServices => "a.filter-badge:has(span:text-is('Sector Facing Services'))";
        public string BA_SkillsAndGrowth => "a.filter-badge:has(span:text-is('Skills and Growth'))";
        public string BA_Strategy => "a.filter-badge:has(span:text-is('Strategy'))";
        public string BA_NotCategorised => "a.filter-badge:has(span:text-is('Not categorised'))";

        // Business Area filter heading
        public string BA_FilterHeading => "//h3[normalize-space()='Business area']";

        // Channel filter tags
        public string Channel_Chat => "a.filter-badge:has(span:text-is('Chat'))";
        public string Channel_Email => "a.filter-badge:has(span:text-is('Email'))";
        public string Channel_FaceToFace => "a.filter-badge:has(span:text-is('Face-to-face'))";
        public string Channel_NativeApp => "a.filter-badge:has(span:text-is('Native app'))";
        public string Channel_OtherDigitalMedia => "a.filter-badge:has(span:text-is('Other digital media'))";
        public string Channel_Phone => "a.filter-badge:has(span:text-is('Phone'))";
        public string Channel_PrintMedia => "a.filter-badge:has(span:text-is('Print media'))";
        public string Channel_SMS => "a.filter-badge:has(span:text-is('SMS'))";
        public string Channel_Web => "a.filter-badge:has(span:text-is('Web'))";
        public string Channel_NotCategorised => "a.filter-badge:has(span:text-is('Not categorised'))";

        // Channel filter heading
        public string Channel_FilterHeading => "//h3[normalize-space()='Channel']";

        // Phase filter tags
        public string Phase_Discovery => "a.filter-badge:has(span:text-is('Discovery'))";
        public string Phase_Alpha => "a.filter-badge:has(span:text-is('Alpha'))";
        public string Phase_DidNotProgress => "a.filter-badge:has(span:text-is('Did not progress'))";
        public string Phase_PrivateBeta => "a.filter-badge:has(span:text-is('Private beta'))";
        public string Phase_PublicBeta => "a.filter-badge:has(span:text-is('Public beta'))";
        public string Phase_Live => "a.filter-badge:has(span:text-is('Live'))";
        public string Phase_Decommissioning => "a.filter-badge:has(span:text-is('Decommissioning'))";
        public string Phase_Decommissioned => "a.filter-badge:has(span:text-is('Decommissioned'))";
        public string Phase_NotCategorised => "a.filter-badge:has(span:text-is('Not categorised'))";

        // Phase filter heading
        public string Phase_FilterHeading => "//h3[normalize-space()='Phase']";

        // Type filter tags
        public string Type_API => "a.filter-badge:has(span:text-is('API'))";
        public string Type_Campaign => "a.filter-badge:has(span:text-is('Campaign'))";
        public string Type_Data => "a.filter-badge:has(span:text-is('Data   '))";
        public string Type_Information => "a.filter-badge:has(span:text-is('Information'))";
        public string Type_Transactional => "a.filter-badge:has(span:text-is('Transactional'))";
        public string Type_NotCategorised => "a.filter-badge:has(span:text-is('Not categorised'))";

        // Type filter heading
        public string Type_FilterHeading => "//h3[normalize-space()='Type']";

        // Search term heading
        public string Search_FilterHeading => "//h3[normalize-space()='Search term']";

        // Search term filter tags
        public string KeywordSearchTag => "a.filter-badge:has(span:text-is('Apprentice'))";
        public string GiasAcronym_FilterTag => "a.filter-badge:has(span:text-is('GIAS'))";
        public string FipsAcronym_FilterTag => "a.filter-badge:has(span:text-is('FIPS'))";
        public string UGTermEYW_FilterTag => "a.filter-badge:has(span:text-is('Early years workforce'))";
        public string UGTermCYP_FilterTag => "a.filter-badge:has(span:text-is('Child or young person'))";
        public string NonPreferTermDfEStaff_FilterTag => "a.filter-badge:has(span:text-is('DfE staff'))";
        public string NonPreferTermHEWorkforce_FilterTag => "a.filter-badge:has(span:text-is('HE workforce'))";
        public string CrmAcronym_FilterTag => "a.filter-badge:has(span:text-is('CRM'))";
        public string NationalCareer_FilterTag => "a.filter-badge:has(span:text-is('National career'))";
        public string AI_FilterTag => "a.filter-badge:has(span:text-is('AI'))";
        public string AndyJones_FilterTag => "a.filter-badge:has(span:text-is('Andy Jones'))";
        public string SamBarton_FilterTag => "a.filter-badge:has(span:text-is('Sam Barton'))";
        public string PrivateBetaSearch_FilterTag => "a.filter-badge:has(span:text-is('private beta'))";
        public string CustExpAndDesignSearch_FilterTag => "a.filter-badge:has(span:text-is('customer experience and design'))";

        // User groups filter tag
        public string UserGroups_FilterTag => "a.filter-badge:has(span:text-is('Department for Education workforce'))";

        // Multi not categorised group
        public string Channel_NotCategorisedGroup => "a[href='/products?group=__not_categorised__&type=__not_categorised__']";        
        public string Type_NotCategorisedGroup => "a[href='/products?group=__not_categorised__&channel=__not_categorised__']";
        public string BA_NotCategorisedGroup => "a[href='/products?channel=__not_categorised__&type=__not_categorised__']";

        public FilterTagsComponent(IPage page)
        {
            this.page = page;
        }

        public async Task VerifyAppliedFiltersPanelContainsAsync(string expectedText)
        {
            await Assertions.Expect(AppliedFiltersPanel).ToContainTextAsync(expectedText);
        }

        public async Task VerifyFilterHeadingAsync(string headingLocator, string expectedText)
        {
            await Assertions.Expect(page.Locator(headingLocator)).ToHaveTextAsync(expectedText);
        }

        public async Task VerifyFilterTagAsync(string tagLocator, string expectedText)
        {
            await Assertions.Expect(page.Locator(tagLocator)).ToHaveTextAsync(expectedText);
        }

        public async Task VerifyFilterTagVisibleAsync(string tagLocator)
        {
            await Assertions.Expect(page.Locator(tagLocator)).ToBeVisibleAsync();
        }

        //public async Task VerifyShowingResultsAsync()
        //{
        //   // await Assertions.Expect(ShowingResultsMessage).ToContainTextAsync("Showing");
        //}
    }
}
