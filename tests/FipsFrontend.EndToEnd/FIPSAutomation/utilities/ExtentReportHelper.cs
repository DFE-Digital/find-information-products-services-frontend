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
                extent.AddSystemInfo("Tester", "Shalini");
                extent.AddSystemInfo("Project", "FiPS");
                extent.AddSystemInfo("Org", "DfE");
                extent.AddSystemInfo("Build-verson", "1.0.12");

                return extent;
            }
        }

        public static void FlushReport()
        {
            extent?.Flush();
        }
    }
}
