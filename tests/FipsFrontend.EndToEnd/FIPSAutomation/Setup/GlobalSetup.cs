using FiPSAutomation.Configuration;
using FiPSAutomation.utilities;
using Microsoft.Playwright;
using System.Text.Json;

namespace FiPSAutomation
{
    [SetUpFixture]
    public class GlobalSetup
    {
        private static SuiteSettings? _settings;

        public static IPlaywright? Playwright { get; private set; }
        public static IBrowser? Browser { get; private set; }
        public static IBrowserContext? Context { get; private set; }
        public static IPage? Page { get; private set; }

        /// <summary>The suite's configuration; see <see cref="SuiteSettings"/> for where it comes from.</summary>
        public static SuiteSettings Settings =>
            _settings ?? throw new InvalidOperationException("The suite's settings are loaded once, before any test, by GlobalSetup.");

        [OneTimeSetUp]
        public async Task RunBeforeAnyTests()
        {
            // Loaded and validated before a browser exists: a wrong or missing value is refused here,
            // naming the key and where it looked, rather than surfacing as "Cannot navigate to invalid
            // URL" from inside the browser's protocol layer with every test reported as failed.
            _settings = SuiteSettings.Load();

            //Initializes Playwright and starts a Chromium browser in headless mode -

            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true, //false,
                Args = new List<string> { "--start-maximized" },
            });
            //Configures the browser context and opens a new page -
            Context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = ViewportSize.NoViewport //Sets viewport size for consistent UI testing.
            });

            // Set once, here, for every test: see TimeoutOptions for why they are configuration.
            Assertions.SetDefaultExpectTimeout(_settings.Timeouts.ExpectMs);
            Context.SetDefaultTimeout(_settings.Timeouts.ActionMs);
            Context.SetDefaultNavigationTimeout(_settings.Timeouts.NavigationMs);

            Page = await Context.NewPageAsync();

            ExtentReportHelper.GetInstance();

            // The report says what was tested: the target as configured, and the version the
            // application itself reports (health/detailed carries "1.0.0+<commit>" and the environment
            // name). A target that does not answer is recorded as such rather than failing the run.
            ExtentReportHelper.extent?.AddSystemInfo("Target", _settings.ApplicationUrl);
            ExtentReportHelper.extent?.AddSystemInfo("Application", await DescribeApplicationAsync(_settings.ApplicationUrl));

            await OpenApplicationAsync(Page, _settings);
        }

        [OneTimeTearDown]
        public async Task RunAfterAllTests()
        {
            if (Browser != null)
            {
                await Browser.CloseAsync(); //Closes the browser and releases Playwright resources
            }
            Playwright?.Dispose();
            ExtentReportHelper.FlushReport(); //Finalizes and flushes reports
        }

        private static async Task<string> DescribeApplicationAsync(string applicationUrl)
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                using var json = JsonDocument.Parse(await http.GetStringAsync(new Uri(new Uri(applicationUrl), "health/detailed")));
                var application = json.RootElement.GetProperty("application");
                return $"{application.GetProperty("informationalVersion").GetString()} ({application.GetProperty("environment").GetString()})";
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
            {
                return $"unknown - health/detailed did not answer as expected ({ex.GetType().Name}: {ex.Message})";
            }
        }

        private static async Task OpenApplicationAsync(IPage page, SuiteSettings settings)
        {
            if (settings.SignIn is null)
            {
                await page.GotoAsync(settings.ApplicationUrl);
            }
            else
            {
                await page.GotoAsync(settings.SignIn.OAuthUrl);
                await page.GetByPlaceholder("Email or phone").ClickAsync();
                await page.GetByPlaceholder("Email or phone").FillAsync(settings.SignIn.UserName);
                await page.GetByRole(AriaRole.Button, new() { NameString = "Next" }).ClickAsync();
                await page.GetByPlaceholder("Password").ClickAsync();
                await page.GetByPlaceholder("Password").FillAsync(settings.SignIn.Password);
                await page.GetByRole(AriaRole.Button, new() { NameString = "Sign in" }).ClickAsync();
                await page.WaitForURLAsync(settings.SignIn.LoginUrl);
                await page.GetByRole(AriaRole.Button, new() { NameString = "Yes" }).ClickAsync();
                await page.WaitForURLAsync(settings.ApplicationUrl);
            }

            await page.GetByRole(AriaRole.Button, new() { NameString = "Accept analytics cookies" }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { NameString = "Hide cookie message" }).ClickAsync();
        }
    }
}
