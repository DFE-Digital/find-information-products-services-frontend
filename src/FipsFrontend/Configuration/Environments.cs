namespace FipsFrontend.Configuration;

/// <summary>
/// The environment names this application distinguishes, and what each is allowed. ASP.NET's own
/// default stands: an unnamed environment is Production and gets nothing below. "Development" is
/// what the hosted development platform sets; "local-dev" is a developer's machine (the launch
/// profiles set it) and the pipeline's copy is "ci". Conveniences are opt-in by name, never the default.
/// </summary>
public static class Environments
{
    /// <summary>A developer's machine: the only place local-only behaviour (static assets from source, a local sign-in) is offered.</summary>
    public const string LocalDev = "local-dev";

    public static bool IsLocalDev(this IHostEnvironment environment) => environment.IsEnvironment(LocalDev);

    /// <summary>
    /// Development, hosted or local: the developer exception page shows, and HSTS is not sent (plain
    /// http is normal on both). Nothing that only a developer's own machine should have keys on this;
    /// that is <see cref="IsLocalDev"/>.
    /// </summary>
    public static bool IsDevelopmentLike(this IHostEnvironment environment) => environment.IsDevelopment() || environment.IsLocalDev();
}
