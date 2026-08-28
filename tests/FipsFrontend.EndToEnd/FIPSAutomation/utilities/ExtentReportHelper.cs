using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;


namespace FiPSAutomation.utilities
{
    internal class ExtentReportHelper
    {
        public static ExtentReports? extent;
        public static ExtentTest? test;
        private static readonly object lockObject = new();

        public static ExtentReports GetInstance() {
            if (extent != null) {
                return extent;
            }

            lock (lockObject) {

                if (extent != null) {
                    return extent;
                }

                extent = new ExtentReports();

                var htmlReporter = new ExtentSparkReporter(Directory.GetParent(Environment.CurrentDirectory)
                .Parent.Parent.FullName + "//playwright-report//" 
                + ("extent-" + DateTime.Now.ToString("yyyy-MM-dd HHmmss") + ".html"));

                htmlReporter.Config.DocumentTitle = "FiPS Automation Report";
                htmlReporter.Config.ReportName = "FIPS Automation";
                htmlReporter.Config.Encoding = "utf-8";
                htmlReporter.Config.Theme = AventStack.ExtentReports.Reporter.Config.Theme.Standard;

                extent = new ExtentReports();
                extent.AttachReporter(htmlReporter);
                extent.AddSystemInfo("OS", Environment.OSVersion.ToString());
                extent.AddSystemInfo("Framework", ".Net + Playwright + NUnit");
                extent.AddSystemInfo(".Net Version", Environment.Version.ToString());
                extent.AddSystemInfo("Browser", "Chromium");
                // Who ran it: the pipeline's actor when there is one, otherwise the signed-in user.
                extent.AddSystemInfo("Run by", Environment.GetEnvironmentVariable("GITHUB_ACTOR") ?? Environment.UserName);
                extent.AddSystemInfo("Project", "FiPS");
                extent.AddSystemInfo("Org", "DfE");
                // The target and the version of the application under test are added by GlobalSetup once it knows them.

                return extent;
            }
        }

        public static void FlushReport()
        {
            extent?.Flush();
        }
    }
}
