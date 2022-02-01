using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPCode.Utils
{
    public static class DirUtils
    {
        public static void ClearTempFolder()
        {
            var tempDir = Paths.GetTempDirectory();
            foreach (var file in Directory.GetFiles(tempDir))
            {
                File.Delete(file);
            }
            foreach (var dir in Directory.GetDirectories(tempDir))
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
