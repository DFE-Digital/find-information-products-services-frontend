using AventStack.ExtentReports;
using FiPSAutomation.Pages;
using FiPSAutomation.Components;
using Microsoft.Playwright;

namespace FiPSAutomation;

[TestFixture, Order(17)]
[Category("Functional"), Category("Integration")]
public class ProductDetailTests : BaseTest
{
    private ProductDetailPage productDetailPage = null!;
    private ProductsSearchPage productsSearchPage = null!;
    private HeaderComponent header = null!;

    [OneTimeSetUp]
    public void InitPages()
    {
        productDetailPage = new ProductDetailPage(Page);
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

        // #168 AC3: a link to access the product directly. The product's address is its own data and the site behind
        // it is not FIPS, so the link's presence and an absolute destination are asserted; the site is not opened.
        var viewProduct = targetValueRow.GetByRole(AriaRole.Link, new() { Exact = true, Name = "View product" });
        await Assertions.Expect(viewProduct).ToBeVisibleAsync();
        await Assertions.Expect(viewProduct).ToHaveAttributeAsync("href", new Regex("^https?://"));
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

        // #168 AC7 to AC7c (the "Propose a change" link and form) no longer apply since #315 (part of #308).

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
        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Exact = true, Name = "Customer Experience and Design" })).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Link, new() { Exact = true, Name = "Customer Experience and Design" }).ClickAsync();
        await productsSearchPage.VerifyProductsPageHeadingAsync();
        // bug raised for below line 177 code, once fixed this TC should pass
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.BA_CustomerExpAndDesign, "Customer Experience and Design × Remove Customer Experience and Design filter");
        await Page.GoBackAsync();

        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Exact = true, Name = "Web" })).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Link, new() { Exact = true, Name = "Web" }).ClickAsync();
        await productsSearchPage.VerifyProductsPageHeadingAsync();
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Channel_Web, "Web × Remove Web filter");
        await Page.GoBackAsync();

        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Exact = true, Name = "Live" })).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Link, new() { Exact = true, Name = "Live" }).ClickAsync();
        await productsSearchPage.VerifyProductsPageHeadingAsync();
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Phase_Live, "Live × Remove Live filter");
        await Page.GoBackAsync();

        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Exact = true, Name = "Information" })).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Link, new() { Exact = true, Name = "Information" }).ClickAsync();
        await productsSearchPage.VerifyProductsPageHeadingAsync();
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.Type_Information, "Information × Remove Information filter");
        await Page.GoBackAsync();

        ExtentTest?.Log(Status.Pass, "ClickSubcategoriesLinkInCategoriesTableUS168AC passed");
    }

    [Test, Order(5)]
    public async Task VerifyLinkInUsersProductTableUS168AC()
    {
        await NavigateToAsync("product/h7pjd1dx4hwvjm9zg6bv2gci/categories");
        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Exact = true, Name = "Department for Education workforce" })).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Link, new() { Exact = true, Name = "Department for Education workforce" }).ClickAsync();
        await productsSearchPage.VerifyProductsPageHeadingAsync();
        await productsSearchPage.FilterTags.VerifyFilterTagAsync(productsSearchPage.FilterTags.UserGroups_FilterTag, "Department for Education workforce × Remove Department for Education workforce filter");
        await Page.GoBackAsync();
        await productDetailPage.VerifySearchTermsListAsync("Search terms");
        await Assertions.Expect(Page.GetByText("DfE Staff", new() { Exact = true})).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByText("DfE workforce", new() { Exact = true })).ToBeVisibleAsync();

        ExtentTest?.Log(Status.Pass, "VerifyLinkInUsersProductTableUS168AC passed");
    }

}
