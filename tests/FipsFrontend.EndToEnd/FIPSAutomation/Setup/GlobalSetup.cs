using find_information_products_services_tests.FIPSAutomation.login;
using FiPSAutomation.utilities;
using Microsoft.Playwright;
using System.Text.Json;

namespace FiPSAutomation
{
    [SetUpFixture]
    public class GlobalSetup
    {
        public static IPlaywright? Playwright { get; private set; }
        public static IBrowser? Browser { get; private set; }
        public static IBrowserContext? Context { get; private set; }
        public static IPage? Page { get; private set; }
        public static LoginConfig? LoginConfig { get; private set; }
        public static EnvironmentDetail? ActiveEnvironment { get; private set; }

        [OneTimeSetUp]
        public async Task RunBeforeAnyTests()
        {
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

            Page = await Context.NewPageAsync();

            ExtentReportHelper.GetInstance();

            await LoginAndAcceptCookiesAsync();
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

        private async Task LoginAndAcceptCookiesAsync()
        {
            // env.json is a tracked template whose values are placeholders, so running against
            // anything meant editing a tracked file - easy to commit by accident, easy to lose to a
            // checkout. env.local.json, if present, is preferred and is gitignored: a machine keeps
            // its own configuration and the template stays a template.
            var configDirectory = Directory.GetParent(Environment.CurrentDirectory)!
                .Parent!.Parent!.FullName;

            var localOverride = Path.Combine(configDirectory, "env.local.json");
            var configPath = File.Exists(localOverride)
                ? localOverride
                : Path.Combine(configDirectory, "env.json");

            using FileStream stream = File.OpenRead(configPath);

            LoginConfig = await JsonSerializer.DeserializeAsync<LoginConfig>(stream);

            if (LoginConfig != null)
            {
                ActiveEnvironment = LoginConfig.Envs.FirstOrDefault(e => e.Env == LoginConfig.ActiveEnv);
            }

            // Refuses here rather than letting the browser report "Cannot navigate to invalid URL"
            // from inside its protocol layer: with the placeholders still in place every test in the
            // run fails before a single assertion executes, which reads as a broken application
            // rather than an unconfigured suite. This names the file read, the environment
            // selected, and the value found - the three things needed to fix it.
            var url = ActiveEnvironment?.ApplicationURL;
            if (string.IsNullOrWhiteSpace(url) || url.StartsWith('<'))
            {
                throw new InvalidOperationException(
                    $"No usable applicationURL for environment '{LoginConfig?.ActiveEnv}' in {configPath}. "
                    + $"Found: '{url}'. env.json ships with placeholders; copy it to env.local.json "
                    + "(gitignored) and set applicationURL there - see README.md.");
            }

            // Set once, here, for every test: see Timeouts in LoginConfig.cs for why they are configuration.
            Assertions.SetDefaultExpectTimeout(LoginConfig!.Timeouts.ExpectMs);
            Context!.SetDefaultTimeout(LoginConfig.Timeouts.ActionMs);
            Context.SetDefaultNavigationTimeout(LoginConfig.Timeouts.NavigationMs);

            if (!LoginConfig!.LoginRequired)
            {
                await Page!.GotoAsync(ActiveEnvironment!.ApplicationURL);
            }
            else
            {
                //await Page!.GotoAsync(ActiveEnvironment!.ApplicationURL);
                await Page!.GotoAsync(ActiveEnvironment!.OAuthURL); // TODO
                try
                {
                    await Page.GetByPlaceholder("Email or phone").ClickAsync();
                    //byte[] usernameBytes = Convert.FromBase64String(Environment.GetEnvironmentVariable("USERNAME"));
                    //string username = Encoding.UTF8.GetString(usernameBytes);
                    ////Console.WriteLine("XXXXXXXXXXXXX: decodedString:"+ username);
                    ////extentTest?.Log(Status.Pass, "decodedString:" + username);
                    //await page.GetByPlaceholder("Email or phone").FillAsync(username);

                    await Page.GetByPlaceholder("Email or phone").FillAsync(StringUtility.Base64Decode(LoginConfig.UserName));
                    await Page.GetByRole(AriaRole.Button, new() { NameString = "Next" }).ClickAsync();

                    await Page.GetByPlaceholder("Password").ClickAsync();
                    //byte[] passwordBytes = Convert.FromBase64String(Environment.GetEnvironmentVariable("PASSWORD"));
                    //string password = Encoding.UTF8.GetString(passwordBytes);
                    //await page.GetByPlaceholder("Password").FillAsync(password);
                    await Page.GetByPlaceholder("Password").FillAsync(StringUtility.Base64Decode(LoginConfig.Password));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error while login :- " + ex.StackTrace);
                    return;
                }
                await Page.GetByRole(AriaRole.Button, new() { NameString = "Sign in" }).ClickAsync();
                await Page.WaitForURLAsync(LoginConfig.LoginURL); // TODO
                await Page.GetByRole(AriaRole.Button, new() { NameString = "Yes" }).ClickAsync();
                await Page.WaitForURLAsync(ActiveEnvironment.ApplicationURL); // TODO
            }

            await Page.GetByRole(AriaRole.Button, new() { NameString = "Accept analytics cookies" }).ClickAsync();
            await Page.GetByRole(AriaRole.Button, new() { NameString = "Hide cookie message" }).ClickAsync();
        }
    }
}
