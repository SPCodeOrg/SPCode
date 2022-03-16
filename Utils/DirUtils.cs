using System.IO;

namespace SPCode.Utils
{
    public static class DirUtils
    {
        public static void ClearTempFolder()
        {
            var tempDir = PathsHelper.TempDirectory;
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
