using Compass.FipsApi.Contracts;

namespace FipsFrontend.Tests;

/// <summary>
/// What a COMPASS holding nothing answers to each kind of request, decided by the path alone: a scenario prefix,
/// a leading slash, or a query string does not change the answer, and a product asked for by id is unknown rather
/// than an empty page.
/// </summary>
[TestFixture]
public class EmptyAnswersTests
{
    [TestCase("api/v1/ServiceRegister/fips/configuration", 200, EmptyAnswers.Bundle, TestName = "EmptyCompass_TheConfigurationBundle_HasEveryVocabularyEmpty")]
    [TestCase("/seeded/api/v1/ServiceRegister/fips", 200, EmptyAnswers.Bundle, TestName = "EmptyCompass_TheBundleUnderAScenarioPrefix_IsStillTheBundle")]
    [TestCase("api/v1/ServiceRegister/products?page=1&pageSize=25&status=Active", 200, EmptyAnswers.Page, TestName = "EmptyCompass_AProductsPageWithAQuery_HasZeroRecords")]
    [TestCase("/empty/api/v1/ServiceRegister/products/9d5a8d3e-0b7e-4d0c-9c1a-2f6b3c4d5e6f", 404, EmptyAnswers.NotFound, TestName = "EmptyCompass_AProductById_IsUnknown")]
    [TestCase("api/v1/ServiceRegister/products/9d5a8d3e-0b7e-4d0c-9c1a-2f6b3c4d5e6f?expand=contacts", 404, EmptyAnswers.NotFound, TestName = "EmptyCompass_AProductByIdWithAQuery_IsStillUnknown")]
    [TestCase("api/v1/ServiceRegister/products/not-a-guid", 200, EmptyAnswers.Page, TestName = "EmptyCompass_AProductsPathWhoseLastSegmentIsNotAnId_IsAPage")]
    [TestCase("api/v1/ServiceRegister/fips/channels", 200, EmptyAnswers.List, TestName = "EmptyCompass_ALookup_IsAnEmptyList")]
    public void EmptyCompass_AnswersByPath(string path, int status, string body)
    {
        var (actualStatus, actualBody) = EmptyAnswers.For(path);

        Assert.That(actualStatus, Is.EqualTo(status));
        Assert.That(actualBody, Is.EqualTo(body));
    }

    [Test]
    public void EmptyCompass_AFullRequestAddress_AnswersTheSameAsItsPath()
    {
        var byUri = EmptyAnswers.For(new Uri("http://compass.example.com/seeded/api/v1/ServiceRegister/products?page=2"));
        var byPath = EmptyAnswers.For("api/v1/ServiceRegister/products");

        Assert.That(byUri, Is.EqualTo(byPath));
    }
}
