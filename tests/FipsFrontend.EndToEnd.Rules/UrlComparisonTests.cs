using FiPSAutomation.Components;

namespace FipsFrontend.EndToEnd.Rules;

/// <summary>
/// The url comparison behind the pagination assertions, without a browser, an environment, or data.
/// The comparison encodes decisions: parameter order ignored, duplicates significant, encodings normalised, the
/// path matched whole. The cases that matter most assert a NON-match: a helper that returns true too readily
/// produces green runs that mean nothing, and a wrong green is never investigated the way a wrong red is.
/// </summary>
[TestFixture]
public class UrlComparisonTests
{
    private const string Base = "https://localhost:7601";

    [Test]
    public void PathAndQuery_WhenIdentical_Matches() =>
        Assert.That(Urls.SamePathAndQuery($"{Base}/Products?type=api&page=2", "/Products?type=api&page=2"), Is.True);

    [Test]
    public void PathAndQuery_WhenOriginDiffers_StillMatches() =>
        Assert.That(Urls.SamePathAndQuery("https://some-hosted-env.example.com/Products?page=2", "/Products?page=2"), Is.True);

    [Test]
    public void QueryParameters_WhenOrderDiffers_StillMatch() =>
        Assert.That(Urls.SamePathAndQuery($"{Base}/Products?type=information&type=api&page=2", "/Products?type=api&type=information&page=2"), Is.True);

    [Test]
    public void QueryParameters_WhenOneIsDuplicated_DoNotMatch() =>
        Assert.That(Urls.SamePathAndQuery($"{Base}/Products?type=api&type=api&page=2", "/Products?type=api&page=2"), Is.False);

    [Test]
    public void QueryParameters_WhenAnExtraIsPresent_DoNotMatch() =>
        Assert.That(Urls.SamePathAndQuery($"{Base}/Products?type=api&page=2&channel=web", "/Products?type=api&page=2"), Is.False);

    [Test]
    public void QueryParameters_WhenOneIsMissing_DoNotMatch() =>
        Assert.That(Urls.SamePathAndQuery($"{Base}/Products?type=api", "/Products?type=api&page=2"), Is.False);

    [Test]
    public void QueryParameters_WhenAValueDiffers_DoNotMatch() =>
        Assert.That(Urls.SamePathAndQuery($"{Base}/Products?type=api&page=3", "/Products?type=api&page=2"), Is.False);

    [Test]
    public void Path_WhenLongerButEndingTheSame_DoesNotMatch() =>
        Assert.That(Urls.SamePathAndQuery($"{Base}/Admin/Products?page=2", "/Products?page=2"), Is.False);

    [Test]
    public void Path_WhenCaseDiffers_StillMatches() =>
        // Routing is case-insensitive: the application renders its links in lower case and the suite's expectations
        // name routes capitalised. One route, two renderings.
        Assert.That(Urls.SamePathAndQuery($"{Base}/products?page=2", "/Products?page=2"), Is.True);

    [Test]
    public void Path_WhenDifferent_DoesNotMatch() =>
        Assert.That(Urls.SamePathAndQuery($"{Base}/Categories?page=2", "/Products?page=2"), Is.False);

    [Test]
    public void QueryParameters_WhenEncodedDifferently_StillMatch() =>
        Assert.That(Urls.SamePathAndQuery($"{Base}/Products?keywords=HE%20workforce", "/Products?keywords=HE+workforce"), Is.True);

    [Test]
    public void QueryParameters_WhenValueCaseDiffers_DoNotMatch() =>
        Assert.That(Urls.SamePathAndQuery($"{Base}/Products?type=API", "/Products?type=api"), Is.False);

    [Test]
    public void BracketedArrayParameters_BehaveLikeAnyRepeatedKey()
    {
        Assert.That(Urls.SamePathAndQuery($"{Base}/Products?name[]=b&name[]=a", "/Products?name[]=a&name[]=b"), Is.True);
        Assert.That(Urls.SamePathAndQuery($"{Base}/Products?name[]=a&name[]=a", "/Products?name[]=a"), Is.False);
        Assert.That(Urls.SamePathAndQuery($"{Base}/Products?name%5B%5D=a", "/Products?name[]=a"), Is.True);
    }

    [Test]
    public void Query_WhenAbsentOnBothSides_Matches() =>
        Assert.That(Urls.SamePathAndQuery($"{Base}/Products", "/Products"), Is.True);

    [Test]
    public void Query_WhenPresentOnOnlyOneSide_DoesNotMatch() =>
        Assert.That(Urls.SamePathAndQuery($"{Base}/Products?page=2", "/Products"), Is.False);
}
