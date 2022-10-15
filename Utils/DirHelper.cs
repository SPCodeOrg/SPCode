using System;
using System.IO;
using System.Reflection;

namespace SPCode.Utils;

public static class DirHelper
{
    public static void ClearSPCodeTempFolder()
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

    public static bool CanAccess(string path)
    {
        try
        {
            Directory.GetAccessControl(path);
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        return true;
    }

    public static bool CanAccess(DirectoryInfo dir)
    {
        try
        {
            dir.GetAccessControl();
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        return true;
    }

    public static bool HasValidTextExtension(FileInfo file)
    {
        return file.Extension
            is ".sp"
            or ".inc"
            or ".txt"
            or ".cfg"
            or ".ini";
    }

    public static bool IsBinary(FileInfo file) => file.Extension is ".smx";
}
