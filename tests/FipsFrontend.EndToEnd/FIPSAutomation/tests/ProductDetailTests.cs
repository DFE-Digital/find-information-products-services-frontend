using AventStack.ExtentReports;
using FiPSAutomation.Pages;
using FiPSAutomation.Components;
using Microsoft.Playwright;

namespace FiPSAutomation;

[TestFixture, Order(17)]
[Category("Functional")]
public class ProductDetailTests : BaseTest
{
    private ProductDetailPage productDetailPage = null!;
    private ProposeChangePage proposeChangePage = null!;
    private ProductsSearchPage productsSearchPage = null!;
    private HeaderComponent header = null!;

    [OneTimeSetUp]
    public void InitPages()
    {
        productDetailPage = new ProductDetailPage(Page);
        proposeChangePage = new ProposeChangePage(Page);
        productsSearchPage = new ProductsSearchPage(Page);
        header = new HeaderComponent(Page);
    }

    [Test, Order(1)]
    public async Task VerifyProductOverviewPageHeadersUS168AC()
    {
        await NavigateToAsync("product/h7pjd1dx4hwvjm9zg6bv2gci");
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Accessibility and inclusion manual" })).ToBeVisibleAsync();
        await productDetailPage.VerifyOverviewHeadersAsync();

        var targetHeader = Page.Locator(productDetailPage.tableHeader)
            .Filter(new() { HasTextString = "Phase" })
            .Filter(new() { HasTextString = "Business area" })
            .Filter(new() { HasTextString = "Contacts" })
            .Filter(new() { HasTextString = "View product" });
        var targetValueRow = Page.Locator(productDetailPage.tableRow)
            .Filter(new() { HasTextString = "Live" })
            .Filter(new() { HasTextString = "Customer Experience and Design" })
            .Filter(new() { HasTextString = "contacts" })
            .Filter(new() { HasTextString = "View product" });
        await Assertions.Expect(targetHeader).ToBeVisibleAsync();
        await Assertions.Expect(targetValueRow).ToBeVisibleAsync();

        // Assert that when clicking on 'contacts' link Contacts description is displayed -
        await targetValueRow.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions {NameRegex = new Regex("contacts")}).ClickAsync();
        await productDetailPage.VerifyResponsibilitiesHeaderAsync();
        await productDetailPage.VerifyServiceOwnerAsync();
        await productDetailPage.VerifyContactsNameLinkAsync();

        // clicking on 'View products', product details page opens in new tab. Failing at this step currently -
        var newTab = await Page.RunAndWaitForPopupAsync(async () =>
        {
            await targetValueRow.GetByRole(AriaRole.Link, new() { NameString = "View product" }).ClickAsync();
        });
        await newTab.WaitForLoadStateAsync();

        // Assertions in the new tab
        await Assertions.Expect(newTab).ToHaveURLAsync(ProductDetailPage.Accessibility_URL);
        await Assertions.Expect(newTab).ToHaveTitleAsync("Accessibility and inclusive design manual | Accessibility manual - Department for Education");
        await Assertions.Expect(newTab.GetByRole(AriaRole.Heading, new() { NameString = "Accessibility and inclusive design manual" })).ToBeVisibleAsync();
        await newTab.CloseAsync();

        // Assertions back on the original page
        await Assertions.Expect(Page).ToHaveTitleAsync("Accessibility and inclusion manual - FIPS");

        ExtentTest?.Log(Status.Pass, "VerifyProductOverviewPageHeadersUS168AC passed");
    }

    [Test, Order(2)]
    public async Task VerifyProductOverviewPageLinksUS168AC()
    {
        await NavigateToAsync("product/h7pjd1dx4hwvjm9zg6bv2gci"); //above TC failing due to 'View products' link change, so added direct navigation
        // Assertion for Overview link
        await productDetailPage.ClickOverviewLinkAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Description" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByText("Standards and guidance for designing and building accessible and inclusive products and services in DfE.")).ToBeVisibleAsync();

        // Assertion for Categories link
        await productDetailPage.ClickCategoriesLinkAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Categories" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.Locator(productDetailPage.CategoriesTable)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Users of this product" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.Locator(productDetailPage.UsersOfProductTable)).ToBeVisibleAsync();

        // Assertion for Propose a change link
        await productDetailPage.ClickProposeChangeLinkAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Propose a change to product details" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByText("Use this form to propose changes to this product's information. An administrator will review your suggestions before they are added.", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByText("This form should only be submitted by a permanent member of DfE staff.", new() { Exact = true })).ToBeVisibleAsync();

        ExtentTest?.Log(Status.Pass, "VerifyProductOverviewPageLinksUS168AC passed");
    }

    [Test, Order(3)]       
    public async Task VerifyCategoriesDetailsInTableUS168AC()
    {
        await productDetailPage.ClickCategoriesLinkAsync();
        var expectedTableData = new List<Dictionary<string, string>>
        {
            new Dictionary<string, string>
            {
                { "Name", "Customer Experience and Design" },
                { "Type", "Business area" },
                { "Description", "Partner with DfE teams to champion user needs and connect, improve and simplify services." }
            },
            new Dictionary<string, string>
            {
                { "Name", "Web" },
                { "Type", "Channel" },
                { "Description", "Real-time text-based communication delivered through web or mobile interfaces, often supporting automated and human interactions." }
            },
            new Dictionary<string, string>
            {
                { "Name", "Live" },
                { "Type", "Phase" },
                { "Description", "Continously improving." }
            },
            new Dictionary<string, string>
            {
                { "Name", "Information" },
                { "Type", "Type" },
                { "Description", "Provide guidance, policy content, or structured information to help people make decisions or understand their responsibilities." }
            }
        };

        await productDetailPage.AssertCategoriesTableAsync(expectedTableData);

        ExtentTest?.Log(Status.Pass, "VerifyCategoriesDetailsInTableUS168AC passed");
    }

   /* [Test, Order(99)]
    public async Task VerifyUsersOfTheProductTableUS168AC()
    {
        var expectedTableData = new List<Dictionary<string, string>>
        {
            new Dictionary<string, string>
            {
                { "Name", "Department for Education workforce\r\n                                    \r\n                                        \r\n                                            \r\n                                                \r\n                                                    Search terms (2)\r\n                                                \r\n                                            \r\n                                            \r\n                                                \r\n                                                        DfE Staff\r\n                                                        DfE workforce" },
            },
        };

        await productDetailPage.AssertUsersTableAsync(expectedTableData);

        ExtentTest?.Log(Status.Pass, "VerifyUsersOfTheProductTableUS168AC passed");
    }*/

    [Test, Order(4)]
    public async Task ClickSubcategoriesLinkInCategoriesTableUS168AC()
    {
        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { NameString = "Customer Experience and Design" })).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Link, new() { NameString = "Customer Experience and Design" }).ClickAsync();
        await productsSearchPage.VerifyProductsPageHeadingAsync();
        // bug raised for below line 177 code, once fixed this TC should pass
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.BA_CustomerExpAndDesign, "Customer Experience and Design × Remove Customer Experience and Design filter");
        await Page.GoBackAsync();

        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { NameString = "Web" })).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Link, new() { NameString = "Web" }).ClickAsync();
        await productsSearchPage.VerifyProductsPageHeadingAsync();
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Channel_Web, "Web × Remove Web filter");
        await Page.GoBackAsync();

        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { NameString = "Live" })).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Link, new() { NameString = "Live" }).ClickAsync();
        await productsSearchPage.VerifyProductsPageHeadingAsync();
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Phase_Live, "Live × Remove Live filter");
        await Page.GoBackAsync();

        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { NameString = "Information" })).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Link, new() { NameString = "Information" }).ClickAsync();
        await productsSearchPage.VerifyProductsPageHeadingAsync();
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Type_Information, "Information × Remove Information filter");
        await Page.GoBackAsync();

        ExtentTest?.Log(Status.Pass, "ClickSubcategoriesLinkInCategoriesTableUS168AC passed");
    }

    [Test, Order(5)]
    public async Task VerifyLinkInUsersProductTableUS168AC()
    {
        await NavigateToAsync("product/h7pjd1dx4hwvjm9zg6bv2gci/categories");
        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { NameString = "Department for Education workforce" })).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Link, new() { NameString = "Department for Education workforce" }).ClickAsync();
        await productsSearchPage.VerifyProductsPageHeadingAsync();
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.UserGroups_FilterTag, "Department for Education workforce × Remove Department for Education workforce filter");
        await Page.GoBackAsync();
        await productDetailPage.VerifySearchTermsListAsync("Search terms");
        await Assertions.Expect(Page.GetByText("DfE Staff", new() { Exact = true})).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByText("DfE workforce", new() { Exact = true })).ToBeVisibleAsync();

        ExtentTest?.Log(Status.Pass, "VerifyLinkInUsersProductTableUS168AC passed");
    }

    [Test, Order(6)]
    public async Task VerifyProposeAChangeFormUS168AC()
    {
        await productDetailPage.ClickProposeChangeLinkAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Product details" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByLabel("Product title")).ToBeVisibleAsync();
        await proposeChangePage.VerifyProductTitleValueAsync("Accessibility and inclusion manual");
        await Assertions.Expect(Page.GetByLabel("Short description")).ToBeVisibleAsync();
        await proposeChangePage.VerifyShortDescriptionValueAsync("Standards and guidance for designing and building accessible and inclusive products and services in DfE.");
        await Assertions.Expect(Page.GetByLabel("Product URL")).ToBeVisibleAsync();
        await proposeChangePage.VerifyProductUrlValueAsync(ProductDetailPage.Product_URL); 
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { NameString = "Product classification" })).ToBeVisibleAsync();
        await proposeChangePage.VerifyFormFieldsAsync();

        ExtentTest?.Log(Status.Pass, "VerifyProposeAChangeFormUS168AC passed");
    }

    [Test, Order(7)]
    [Ignore("This test triggers product update email to FIPS Inbox. So, skipped for now")]
    public async Task EditAndSubmitProposeAChangeFormUS168AC()
    {
        await proposeChangePage.FillProductTitleAsync("Automation Test - Accessibility and inclusion manual");
        await proposeChangePage.FillShortDescriptionAsync("Automation Test - Validating that 'Short description' textbox can contain more than 500 characters and there is No limit on characters. Entering description - Standards and guidance for designing and building accessible and inclusive products and services in DfE. Standards and guidance for designing and building accessible and inclusive products and services in DfE. Standards and guidance for designing and building accessible and inclusive products and services in DfE. Standards and guidance for designing and building accessible and inclusive products and services in DfE - End of description test");
        await proposeChangePage.FillProductUrlAsync(ProductDetailPage.Accessibility_URL_Test);
        await proposeChangePage.EditFormClassificationsAsync();
        await proposeChangePage.FillAdditionalInfoAsync("Additional Info - adding automation test case for editing the form");
        await proposeChangePage.FillTeamRolesAsync("Automation Delivery Manager", "Automation Info Asset Owner", "Automation Product Manager", "Automation Senior Resp Officer");
        await proposeChangePage.FillReasonAsync("Running the automation test for the proposed change form edit and submission scenario.");
        await Task.Delay(1000);
        await proposeChangePage.SubmitChangesAsync();
        await proposeChangePage.VerifySuccessMessageAsync();
        await proposeChangePage.VerifySuccessMessageTextAsync("Your proposed changes have been submitted. The FIPS team may contact you if additional action or information is needed.");

        ExtentTest?.Log(Status.Pass, "EditAndSubmitProposeAChangeFormUS168AC passed");
    }

    [Test, Order(8)]
    public async Task EditProposeAChangeFormAndClickCancelUS168AC()
    {
        await productDetailPage.ClickProposeChangeLinkAsync();
        await proposeChangePage.FillProductTitleAsync("Automation Test - Accessibility and inclusion manual");
        await proposeChangePage.FillShortDescriptionAsync("Automation Test - Validating that 'Short description' textbox can contain more than 500 characters and there is No limit on characters. Entering description - Standards and guidance for designing and building accessible and inclusive products and services in DfE. Standards and guidance for designing and building accessible and inclusive products and services in DfE. Standards and guidance for designing and building accessible and inclusive products and services in DfE. Standards and guidance for designing and building accessible and inclusive products and services in DfE - End of description test");
        await proposeChangePage.FillProductUrlAsync(ProductDetailPage.Accessibility_URL_Test);
        await proposeChangePage.EditFormClassificationsAsync();
        await proposeChangePage.FillAdditionalInfoAsync("Additional Info - adding automation test case for editing the form");
        await proposeChangePage.FillTeamRolesAsync("Automation Delivery Manager", "Automation Info Asset Owner", "Automation Product Manager", "Automation Senior Resp Officer");
        await proposeChangePage.FillReasonAsync("Running the automation test for the proposed change edit and submission scenario.");
        await Task.Delay(1000);
        await proposeChangePage.CancelFormAsync();
        await Assertions.Expect(Page).ToHaveTitleAsync("Accessibility and inclusion manual - FIPS");
        await productDetailPage.ClickProposeChangeLinkAsync();
        await proposeChangePage.VerifyProductTitleValueAsync("Accessibility and inclusion manual");

        ExtentTest?.Log(Status.Pass, "EditProposeAChangeFormAndClickCancelUS168AC passed");
    }

}
