using System.Text.Json.Serialization;

namespace find_information_products_services_tests.FIPSAutomation.login
{
    public class LoginConfig
    {
        [JsonPropertyName("activeEnv")]
        public string ActiveEnv { get; set; }

        [JsonPropertyName("loginRequired")]
        public bool LoginRequired { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("loginURL")]
        public string LoginURL { get; set; }

        [JsonPropertyName("envs")]
        public List<EnvironmentDetail> Envs { get; set; } = new();

        [JsonPropertyName("timeouts")]
        public Timeouts Timeouts { get; set; } = new();
    }

    /// <summary>
    /// How long the suite waits, in milliseconds, before an assertion, an action, or a navigation
    /// fails. The defaults are Playwright's own. They are configuration because a failing
    /// assertion waits the whole expect timeout and a page that never appears waits the whole
    /// navigation timeout: against an application on the same machine, which answers in
    /// milliseconds, the defaults make a run of this suite mostly waiting, so env.local.json sets
    /// them low there, while a hosted environment keeps them.
    /// </summary>
    public class Timeouts
    {
        [JsonPropertyName("expectMs")]
        public float ExpectMs { get; set; } = 5_000;

        [JsonPropertyName("actionMs")]
        public float ActionMs { get; set; } = 30_000;

        [JsonPropertyName("navigationMs")]
        public float NavigationMs { get; set; } = 30_000;
    }

    public class EnvironmentDetail
    {
        [JsonPropertyName("env")]
        public string Env { get; set; }

        [JsonPropertyName("applicationURL")]
        public string ApplicationURL { get; set; }

        [JsonPropertyName("oAuthURL")]
        public string OAuthURL { get; set; }
    }
}
