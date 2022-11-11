using System;
using System.IO;

namespace SPCode.Utils;

public static class PathsHelper
{
#if BETA
    private static readonly string SPCodeAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\spcodebeta";
#else
    private static readonly string SPCodeAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\spcode";
#endif
    public static readonly bool LocalInstallation = Directory.Exists(".\\sourcepawn");

    public static string ConfigsDirectory
    {
        get
        {
            var appDataPath = SPCodeAppDataPath + @"\sourcepawn\configs\sm_1_10_0_6509";
            var localPath = @".\sourcepawn\configs\sm_1_10_0_6509";
            return LocalInstallation ? localPath : appDataPath;
        }
    }

    public static string LysisDirectory
    {
        get
        {
            var appDataPath = SPCodeAppDataPath + @"\lysis";
            var localPath = @".\lysis";
            return LocalInstallation ? localPath : appDataPath;
        }
    }

    public static string CrashLogDirectory
    {
        get
        {
            var appDataPath = SPCodeAppDataPath + @"\crashlogs";
            var localPath = @".\crashlogs";
            if (LocalInstallation && !Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
                return localPath;
            }
            return LocalInstallation ? localPath : appDataPath;
        }
    }

    public static string TempDirectory
    {
        get
        {
            var appDataPath = SPCodeAppDataPath + @"\sourcepawn\temp";
            var localPath = @".\sourcepawn\temp";
            return LocalInstallation ? localPath : appDataPath;
        }
    }

    public static string TemplatesDirectory
    {
        get
        {
            var appDataPath = SPCodeAppDataPath + @"\sourcepawn\templates";
            var localPath = @".\sourcepawn\templates";
            return LocalInstallation ? localPath : appDataPath;
        }
    }

    public static string TranslationsDirectory
    {
        get
        {
            var appDataPath = SPCodeAppDataPath + @"\translations";
            var localPath = @".\translations";
            return LocalInstallation ? localPath : appDataPath;
        }
    }

    public static string ConfigFilePath
    {
        get
        {
            var appDataPath = SPCodeAppDataPath + @"\sourcepawn\configs\Configs.xml";
            var localPath = @".\sourcepawn\configs\Configs.xml";
            return LocalInstallation ? localPath : appDataPath;
        }
    }

    public static string TemplatesFilePath
    {
        get
        {
            var appDataPath = SPCodeAppDataPath + @"\sourcepawn\templates\Templates.xml";
            var localPath = @".\sourcepawn\templates\Templates.xml";
            return LocalInstallation ? localPath : appDataPath;
        }
    }

    public static string OptionsFilePath
    {
        get
        {
            var appDataPath = SPCodeAppDataPath + @"\options_0.dat";
            var localPath = @".\options_0.dat";
            return LocalInstallation ? localPath : appDataPath;
        }
    }
}