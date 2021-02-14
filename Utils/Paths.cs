using System;
using System.IO;

namespace SPCode.Utils
{
    public class Paths
    {
        public Paths() { }

        private static readonly string SPCodeAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\spcode";
        private static bool LocalInstallation;

        public static bool IsLocalInstallation()
        {
            var localConfigsPath = @".\sourcepawn\configs\Configs.xml";
            var localTemplatesPath = @".\sourcepawn\templates\Templates.xml";
            if (File.Exists(localTemplatesPath) && File.Exists(localConfigsPath))
            {
                LocalInstallation = true;
                return true;
            }
            else
            {
                LocalInstallation = false;
                return false;
            }
        }

        public static string GetConfigsDirectory()
        {
            var appDataPath = SPCodeAppDataPath + @"\sourcepawn\configs\sm_1_10_0_6478";
            var localPath = @".\sourcepawn\configs\sm_1_10_0_6478";
            return LocalInstallation ? localPath : appDataPath;
        }

        public static string GetLysisDirectory()
        {
            var appDataPath = SPCodeAppDataPath + @"\lysis";
            var localPath = @".\lysis";
            return LocalInstallation ? localPath : appDataPath;
        }

        public static string GetCrashLogDirectory()
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

        public static string GetTempDirectory()
        {
            var appDataPath = SPCodeAppDataPath + @"\sourcepawn\temp";
            var localPath = @".\sourcepawn\temp";
            return LocalInstallation ? localPath : appDataPath;
        }

        public static string GetTemplatesDirectory()
        {
            var appDataPath = SPCodeAppDataPath + @"\sourcepawn\templates";
            var localPath = @".\sourcepawn\templates";
            return LocalInstallation ? localPath : appDataPath;
        }

        public static string GetConfigFilePath()
        {
            var appDataPath = SPCodeAppDataPath + @"\sourcepawn\configs\Configs.xml";
            var localPath = @".\sourcepawn\configs\Configs.xml";
            return LocalInstallation ? localPath : appDataPath;
        }

        public static string GetTemplatesFilePath()
        {
            var appDataPath = SPCodeAppDataPath + @"\sourcepawn\templates\Templates.xml";
            var localPath = @".\sourcepawn\templates\Templates.xml";
            return LocalInstallation ? localPath : appDataPath;
        }

        public static string GetOptionsFilePath()
        {
            var appDataPath = SPCodeAppDataPath + @"\options_0.dat";
            var localPath = @".\options_0.dat";
            return LocalInstallation ? localPath : appDataPath;
        }
    }
}
