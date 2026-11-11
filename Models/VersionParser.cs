using System.Reflection;

namespace ucc.Models.Settings;

public class VersionParser
{
    public static string GetAppVersion()
    {
        string? version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        if (version == null)
            return "???";

        string[]? parts = version.Split('.');
        version = string.Join(".", parts.Take(3));
        return $"v{version}";
    }
}
