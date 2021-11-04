using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using SPCode.UI;
using static SPCode.Utils.JavaInstallation;

namespace SPCode.Utils;

public class DecompileUtil
{
    public async Task DecompilePlugin(string filePath = null)
    {
        var java = new JavaInstallation();
        var fileToDecompile = "";

        // First we check the java version of the user, and act accordingly
        ProgressDialogController checkingJavaDialog = null;
        if (Program.MainWindow != null)
        {
            checkingJavaDialog = await Program.MainWindow.ShowProgressAsync(Program.Translations.Get("JavaInstallCheck") + "...",
                "", false, Program.MainWindow.MetroDialogOptions);
            MainWindow.ProcessUITasks();
        }
        switch (java.GetJavaStatus())
        {
            case JavaResults.Absent:
                {
                    // If java is not installed, offer to download it
                    await checkingJavaDialog.CloseAsync();
                    if (await Program.MainWindow.ShowMessageAsync(Program.Translations.Get("JavaNotFoundTitle"),
                        Program.Translations.Get("JavaNotFoundMessage"),
                        MessageDialogStyle.AffirmativeAndNegative, Program.MainWindow.MetroDialogOptions) == MessageDialogResult.Affirmative)
                    {
                        await java.InstallJava();
                    }
                    return;
                }
            case JavaResults.Outdated:
                {
                    // If java is outdated, offer to upgrade it
                    await checkingJavaDialog.CloseAsync();
                    if (await Program.MainWindow.ShowMessageAsync(Program.Translations.Get("JavaOutdatedTitle"),
                         Program.Translations.Get("JavaOutdatedMessage"),
                         MessageDialogStyle.AffirmativeAndNegative, Program.MainWindow.MetroDialogOptions) == MessageDialogResult.Affirmative)
                    {
                        await java.InstallJava();
                    }
                    return;
                }
            case JavaResults.Correct:
                {
                    // Move on
                    await checkingJavaDialog.CloseAsync();
                    break;
                }
        }

        if (filePath == null)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Sourcepawn Plugins (*.smx)|*.smx",
                Title = Program.Translations.Get("ChDecomp")
            };
            var result = ofd.ShowDialog();
            fileToDecompile = result.Value && !string.IsNullOrWhiteSpace(ofd.FileName) ? ofd.FileName : null;
        }
        else
        {
            fileToDecompile = filePath;
        }

        if (!string.IsNullOrWhiteSpace(fileToDecompile))
        {
            var fInfo = new FileInfo(fileToDecompile);
            if (fInfo.Exists)
            {
                ProgressDialogController task = null;
                if (Program.MainWindow != null)
                {
                    task = await Program.MainWindow.ShowProgressAsync(Program.Translations.Get("Decompiling") + "...",
                        fInfo.FullName, false, Program.MainWindow.MetroDialogOptions);
                    MainWindow.ProcessUITasks();
                }

                // Prepare Lysis execution
                var destFile = fInfo.FullName + ".sp";
                var standardOutput = new StringBuilder();
                using var process = new Process();
                process.StartInfo.WorkingDirectory = Paths.GetLysisDirectory();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.FileName = "java";

                process.StartInfo.Arguments = $"-jar lysis-java.jar \"{fInfo.FullName}\"";

                // Execute Lysis, read and store output
                try
                {
                    process.Start();
                    while (!process.HasExited)
                    {
                        standardOutput.Append(process.StandardOutput.ReadToEnd());
                    }
                    standardOutput.Append(process.StandardOutput.ReadToEnd());
                    File.WriteAllText(destFile, standardOutput.ToString(), Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    await Program.MainWindow.ShowMessageAsync($"{fInfo.Name} {Program.Translations.Get("FailedToDecompile")}",
                        $"{ex.Message}", MessageDialogStyle.Affirmative,
                    Program.MainWindow.MetroDialogOptions);
                }

                // Load the decompiled file to SPCode
                Program.MainWindow.TryLoadSourceFile(destFile, out _, true, false, true);
                if (task != null)
                {
                    await task.CloseAsync();
                }
            }
        }
    }
}
