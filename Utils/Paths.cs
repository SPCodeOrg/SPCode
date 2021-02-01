using System;
using System.IO;

namespace SPCode.Utils
{
    public class Paths
    {
        public Paths() { }

        private static readonly string SPCodeAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\spcode";

        public static bool IsLocalInstallation()
        {
            var localConfigsPath = @".\sourcepawn\configs\Configs.xml";
            var localTemplatesPath = @".\sourcepawn\templates\Templates.xml";
            return File.Exists(localTemplatesPath) && File.Exists(localConfigsPath);
        }

        public static string GetConfigsFolderPath()
        {
            var appDataPath = SPCodeAppDataPath + @"\sourcepawn\configs\sm_1_10_0_6478";
            var localPath = @".\sourcepawn\configs\sm_1_10_0_6478";
            return IsLocalInstallation() ? localPath : appDataPath;
        }

        public static string GetConfigFilePath()
        {
            var appDataPath = SPCodeAppDataPath + @"\sourcepawn\configs\Configs.xml";
            var localPath = @".\sourcepawn\configs\Configs.xml";
            return IsLocalInstallation() ? localPath : appDataPath;
        }

        public static string GetTemplatesFilePath()
        {
            var appDataPath = SPCodeAppDataPath + @"\spcode\sourcepawn\templates\Templates.xml";
            var localPath = @".\sourcepawn\templates\Templates.xml";
            return IsLocalInstallation() ? localPath : appDataPath;
        }

        public static string GetOptionsFilePath()
        {
            return IsLocalInstallation() ? @".\options_0.dat" : SPCodeAppDataPath + @"\options_0.dat";
        }
    }
}
