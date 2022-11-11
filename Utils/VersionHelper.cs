using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SPCode.Utils;

public static class VersionHelper
{
    public static Version GetAssemblyVersion()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version;
    }

    public static string GetAssemblyInformationalVersion()
    {
        return ((AssemblyInformationalVersionAttribute)Assembly.GetExecutingAssembly()
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
            .FirstOrDefault()).InformationalVersion;
    }

    public static int GetRevisionNumber()
    {
        var revRegex = new Regex("(?<=beta)[0-9]*");
        var attribute = (AssemblyInformationalVersionAttribute)Assembly.GetExecutingAssembly()
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false).FirstOrDefault();
        if (!revRegex.IsMatch(attribute.InformationalVersion))
        {
            throw new Exception("No match found for specified informational version.");
        }
        return int.Parse(revRegex.Match(attribute.InformationalVersion).ToString());
    }

    public static int GetRevisionNumber(string informationalVersion)
    {
        var revRegex = new Regex("(?<=beta)[0-9]*");
        if (!revRegex.IsMatch(informationalVersion))
        {
            throw new Exception("No match found for specified informational version.");
        }
        return int.Parse(revRegex.Match(informationalVersion).ToString());
    }
}
