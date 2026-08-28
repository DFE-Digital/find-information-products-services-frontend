namespace FipsFrontend.Models;

public class EnabledFeatures
{
    public bool Assurance { get; set; } = false;
}

public class AppSettings
{
    public EnabledFeatures EnabledFeatures { get; set; } = new EnabledFeatures();
}
