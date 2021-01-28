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

        public static string ConfigPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\spcode\\sourcepawn\\configs\\Configs.xml";
        public static string GitHubNewIssueLink = "https://github.com/Hexer10/SPCode/issues/new";
        public static string SMAPILink = "";
    }
}
