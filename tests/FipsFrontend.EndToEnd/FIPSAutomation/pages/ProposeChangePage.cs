using Microsoft.Playwright;

namespace FiPSAutomation.Pages
{
    public class ProposeChangePage
    {
        private readonly IPage page;

        private ILocator ProposedForm => page.Locator("//form[@method='post']");
        private ILocator ProductTitleTextbox => page.Locator("#ProposedTitle");
        private ILocator ShortDescriptionTextbox => page.Locator("#ProposedShortDescription");
        private ILocator ProductUrlTextbox => page.Locator("#ProposedProductUrl");
        private ILocator ClassificationPhase => page.Locator("legend:has-text(\"Phase\")");
        private ILocator ClassificationBusinessArea => page.Locator("legend:has-text(\"Business area\")");
        private ILocator ClassificationChannels => page.Locator("legend:has-text(\"Channels\")");
        private ILocator ClassificationTypes => page.Locator("legend:has-text(\"Types\")");
        private ILocator SelectedPhaseRadio => page.Locator("#ProposedPhaseId_389");
        private ILocator SelectedBusinessAreaRadio => page.Locator("#ProposedGroupId_445");
        private ILocator SelectedChannelCheckbox => page.Locator("#ProposedChannelIds_375");
        private ILocator SelectedTypeCheckbox => page.Locator("#ProposedTypeIds_393");
        private ILocator AdditionalInfoTextbox => page.Locator("#ProposedUserDescription");
        private ILocator DeliveryManagerTextbox => page.Locator("#ProposedDeliveryManager");
        private ILocator InfoAssetOwnerTextbox => page.Locator("#ProposedInformationAssetOwner");
        private ILocator ProductManagerTextbox => page.Locator("#ProposedProductManager");
        private ILocator SeniorRespOfficerTextbox => page.Locator("#ProposedServiceOwner");
        private ILocator ReasonHintText => page.Locator("#Reason-hint");
        private ILocator ReasonForChangeTextbox => page.Locator("#Reason");
        private ILocator SubmitChangeButton => page.Locator("#submit-changes-btn");
        private ILocator ChangedPhaseRadio => page.Locator("#ProposedPhaseId_388");
        private ILocator ChangedBusinessAreaRadio => page.Locator("#ProposedGroupId_429");
        private ILocator AddedChannelCheckbox => page.Locator("#ProposedChannelIds_379");
        private ILocator AddedTypeCheckbox => page.Locator("#ProposedTypeIds_453");
        private ILocator SuccessMessageAlert => page.Locator(".govuk-notification-banner.govuk-notification-banner--success");

        public ProposeChangePage(IPage page)
        {
            this.page = page;
        }

        public async Task VerifyFormFieldsAsync()
        {
            await Assertions.Expect(ProposedForm).ToBeVisibleAsync();
            await Assertions.Expect(ProductTitleTextbox).ToBeVisibleAsync();
            await Assertions.Expect(ShortDescriptionTextbox).ToBeVisibleAsync();
            await Assertions.Expect(ProductUrlTextbox).ToBeVisibleAsync();
            await Assertions.Expect(ClassificationPhase).ToBeVisibleAsync();
            await Assertions.Expect(ClassificationBusinessArea).ToBeVisibleAsync();
            await Assertions.Expect(ClassificationChannels).ToBeVisibleAsync();
            await Assertions.Expect(ClassificationTypes).ToBeVisibleAsync();
            await Assertions.Expect(SelectedPhaseRadio).ToBeCheckedAsync();
            await Assertions.Expect(SelectedBusinessAreaRadio).ToBeCheckedAsync();
            await Assertions.Expect(SelectedChannelCheckbox).ToBeCheckedAsync();
            await Assertions.Expect(SelectedTypeCheckbox).ToBeCheckedAsync();
            await Assertions.Expect(AdditionalInfoTextbox).ToBeVisibleAsync();
            await Assertions.Expect(DeliveryManagerTextbox).ToBeVisibleAsync();
            await Assertions.Expect(InfoAssetOwnerTextbox).ToBeVisibleAsync();
            await Assertions.Expect(ProductManagerTextbox).ToBeVisibleAsync();
            await Assertions.Expect(SeniorRespOfficerTextbox).ToBeVisibleAsync();
            await Assertions.Expect(ReasonHintText).ToBeVisibleAsync();
            await Assertions.Expect(ReasonForChangeTextbox).ToBeVisibleAsync();
            await Assertions.Expect(SubmitChangeButton).ToBeVisibleAsync();
        }

        public async Task EditAndSubmitFormAsync()
        {
            await ChangedPhaseRadio.CheckAsync();
            await ChangedBusinessAreaRadio.CheckAsync();
            await AddedChannelCheckbox.CheckAsync();
            await AddedTypeCheckbox.CheckAsync();
            await ReasonForChangeTextbox.FillAsync("Automation test - proposing a change");
            await SubmitChangeButton.ClickAsync();
        }

        public async Task EditAndCancelFormAsync()
        {
            await ChangedPhaseRadio.CheckAsync();
            await ChangedBusinessAreaRadio.CheckAsync();
            await page.GetByRole(AriaRole.Link, new() { NameString = "Cancel" }).ClickAsync();
        }

        public async Task VerifySuccessMessageAsync()
        {
            await Assertions.Expect(SuccessMessageAlert).ToBeVisibleAsync();
        }

        public async Task VerifySuccessMessageTextAsync(string expectedText)
        {
            await Assertions.Expect(SuccessMessageAlert).ToContainTextAsync(expectedText);
        }

        public async Task VerifyProductTitleValueAsync(string expectedValue)
        {
            await Assertions.Expect(ProductTitleTextbox).ToHaveValueAsync(expectedValue);
        }

        public async Task VerifyShortDescriptionValueAsync(string expectedValue)
        {
            await Assertions.Expect(ShortDescriptionTextbox).ToHaveValueAsync(expectedValue);
        }

        public async Task VerifyProductUrlValueAsync(string expectedValue)
        {
            await Assertions.Expect(ProductUrlTextbox).ToHaveValueAsync(expectedValue);
        }

        public async Task FillProductTitleAsync(string text)
        {
            await ProductTitleTextbox.FillAsync(text);
        }

        public async Task FillShortDescriptionAsync(string text)
        {
            await ShortDescriptionTextbox.FillAsync(text);
        }

        public async Task FillProductUrlAsync(string url)
        {
            await ProductUrlTextbox.FillAsync(url);
        }

        public async Task FillAdditionalInfoAsync(string text)
        {
            await AdditionalInfoTextbox.FillAsync(text);
        }

        public async Task FillTeamRolesAsync(string deliveryManager, string infoAssetOwner, string productManager, string seniorRespOfficer)
        {
            await DeliveryManagerTextbox.FillAsync(deliveryManager);
            await InfoAssetOwnerTextbox.FillAsync(infoAssetOwner);
            await ProductManagerTextbox.FillAsync(productManager);
            await SeniorRespOfficerTextbox.FillAsync(seniorRespOfficer);
        }

        public async Task FillReasonAsync(string text)
        {
            await ReasonForChangeTextbox.FillAsync(text);
        }

        public async Task EditFormClassificationsAsync()
        {
            await ChangedPhaseRadio.CheckAsync();
            await ChangedBusinessAreaRadio.CheckAsync();
            await AddedChannelCheckbox.CheckAsync();
            await AddedTypeCheckbox.CheckAsync();
        }

        public async Task SubmitChangesAsync()
        {
            await SubmitChangeButton.ClickAsync();
        }

        public async Task CancelFormAsync()
        {
            await page.GetByRole(AriaRole.Link, new() { NameString = "Cancel" }).ClickAsync();
        }
    }
}
