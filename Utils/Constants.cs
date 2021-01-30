using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPCode.Utils
{
    public class Constants
    {
        public Constants() { }

        // I believe we need to implement functions here or refactor this solution to make dynamic directory checks for the different execution environments:
        // - debugging (VS)
        // - standalone installer (all configs go to appdata and need to be fetched there)
        // - portable version (keep everything in the same root folder)

        public static string SPCodeAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\spcode\\";
        public static string ConfigFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\spcode\\sourcepawn\\configs\\Configs.xml";
        public static string OptionsFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\spcode\\options_0.dat";
        public static string TemplatesFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\spcode\\sourcepawn\\templates\\Templates.xml";
        public static string GitHubNewIssueLink = "https://github.com/Hexer10/SPCode/issues/new";
        public static string SMAPILink = "";
    }
}
