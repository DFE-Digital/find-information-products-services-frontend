using System.Net;
using FipsFrontend.Tests.TestSupport;

namespace FipsFrontend.Tests;

/// <summary>
/// Nothing in this service writes product information any more: the forms (#315) and the admin
/// edit area are gone, their routes answer nothing, and the guidance points people at the team.
/// </summary>
[TestFixture]
public class RemovedFormsTests
{
    private FipsApplication _app = null!;

    [OneTimeSetUp]
    public void HostTheApplication() => _app = new FipsApplication();

    [OneTimeTearDown]
    public void StopTheApplication() => _app.Dispose();

    [TestCase("/product/FIPS-0001/propose-change")]
    [TestCase("/products/requestnewentry")]
    [TestCase("/admin/productcreate")]
    [TestCase("/admin")]
    [TestCase("/admin/productmanage")]
    [TestCase("/admin/cmdbmatching")]
    [TestCase("/product/FIPS-0001/edit")]
    public async Task RemovedForm_WhenRequested_IsNotFound(string path)
    {
        var response = await _app.Client.GetAsync(path);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), path);
    }

    [Test]
    public async Task ProductListing_WhenThereAreNoProducts_SaysSoOnceAndOffersNoCreateLink()
    {
        var html = await _app.Client.GetStringAsync("/products");

        Assert.That(html, Does.Contain("There are no products and services to show."));
        Assert.That(html, Does.Not.Contain("Add the first product"));
        Assert.That(html, Does.Not.Contain("No products found matching your filters"));
    }

    /// <summary>
    /// The wording that replaces the forms is interim (#314 will direct people to COMPASS), so this
    /// holds only what outlives it: the page offers neither form, and it offers a way forward.
    /// </summary>
    [Test]
    public async Task UpdatesPage_WhenRequested_OffersNoFormButAWayForward()
    {
        var html = await _app.Client.GetStringAsync("/updates");

        Assert.That(html, Does.Not.Contain("Propose a change"));
        Assert.That(html, Does.Not.Contain("request a new product entry"));
        Assert.That(html, Does.Match("""<h2[^>]*>Update a product or service</h2>\s*<p[^>]*>(?s:.*?)<a [^>]*href="/[^"]*"(?s:.*?)</p>"""),
            "the 'Update a product or service' section should contain a link to somewhere");
    }
}
