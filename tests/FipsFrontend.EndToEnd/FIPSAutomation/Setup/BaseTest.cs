using AventStack.ExtentReports;
using find_information_products_services_tests.FIPSAutomation.login;
using FiPSAutomation.utilities;
using Microsoft.Playwright;

namespace FiPSAutomation
{
    public abstract class BaseTest
    {
        protected IPage Page => GlobalSetup.Page!;
        protected ExtentTest? ExtentTest;
        protected EnvironmentDetail ActiveEnvironment => GlobalSetup.ActiveEnvironment!;

        [SetUp]
        public void SetUp()
        {
            ExtentTest = ExtentReportHelper.extent?.CreateTest(TestContext.CurrentContext.Test.Name);
        }

        [TearDown]
        public async Task TearDown()
        {
            var result = TestContext.CurrentContext.Result;
            var status = result.Outcome.Status;

            if (status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                // 1. Capture Full Scrollable Page screenshot
                byte[] screenshot = await Page.ScreenshotAsync(new PageScreenshotOptions
                {
                    FullPage = true
                    
                });
                string base64Screenshot = Convert.ToBase64String(screenshot);

                // 2. Retrieve URL and failure details
                string currentUrl = Page.Url;
                string errorMessage = result.Message;
                string stackTrace = result.StackTrace;

                // 3. Log to Extent Report
                // Log the URL and Error Message as a header
                ExtentTest?.Fail($"<b>Failed URL:</b> <a href='{currentUrl}' target='_blank'>{currentUrl}</a><br/>" +
                                $"<b>Error:</b> {errorMessage}",
                    MediaEntityBuilder.CreateScreenCaptureFromBase64String(base64Screenshot).Build());

                // 4. Log the Stack Trace in a formatted block
                ExtentTest?.Log(Status.Fail, $"Stack Trace: <pre>{stackTrace}</pre>");
            }
            else if (status == NUnit.Framework.Interfaces.TestStatus.Passed)
            {
                ExtentTest?.Pass("Test passed.");
            }
            else
            {
                ExtentTest?.Skip("Test skipped");
            }
        }


        protected async Task NavigateToAsync(string path)
        {
            await Page.GotoAsync(ActiveEnvironment.ApplicationURL + path);
        }

        protected void LogStep(string message)
        {
            ExtentTest?.Info(message);
        }
    }
}
