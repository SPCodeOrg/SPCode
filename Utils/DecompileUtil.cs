using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Win32;
using static SPCode.Interop.TranslationProvider;

namespace SPCode.Utils;

public static class DecompileUtil
{
    public static FileInfo GetFile()
    {
        string fileToDecompile;

        var ofd = new OpenFileDialog
        {
            Filter = Constants.DecompileFileFilters,
            Title = Translate("ChDecomp")
        };
        var result = ofd.ShowDialog();
        fileToDecompile = result.Value && !string.IsNullOrWhiteSpace(ofd.FileName) ? ofd.FileName : null;

        if (fileToDecompile == null)
        {
            return null;
        }
        return new FileInfo(fileToDecompile);
    }

    public static string GetDecompiledPlugin(FileInfo file)
    {
        var decompilerPath = PathsHelper.LysisDirectory;
        var destFile = file.FullName + ".sp";
        var standardOutput = new StringBuilder();
        using var process = new Process();
        var si = new ProcessStartInfo
        {
            WorkingDirectory = decompilerPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            FileName = "cmd.exe",
            Arguments = $"/c LysisDecompiler.exe \"{file.FullName}\"",
        };
        process.StartInfo = si;

        if (File.Exists(destFile))
        {
            File.Delete(destFile);
        }

        try
        {
            process.Start();
            standardOutput.Append(process.StandardOutput.ReadToEnd());
            File.WriteAllText(destFile, standardOutput.ToString(), Encoding.UTF8);
        }
        catch (Exception)
        {
            throw;
        }

        return destFile;
    }
}