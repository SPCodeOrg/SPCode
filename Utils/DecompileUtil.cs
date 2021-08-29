using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using SPCode.UI;
using static SPCode.Utils.JavaInstallation;

namespace SPCode.Utils
{
    public class DecompileUtil
    {
        private readonly MainWindow _win;
        private readonly MetroDialogSettings _metroDialogOptions;

        public DecompileUtil()
        {
            _win = Program.MainWindow;
            _metroDialogOptions = _win.MetroDialogOptions;
        }

        public async Task DecompilePlugin()
        {
            var java = new JavaInstallation();

            // First we check the java version of the user, and act accordingly

            ProgressDialogController checkingJavaDialog = null;
            if (_win != null)
            {
                checkingJavaDialog = await _win.ShowProgressAsync(Program.Translations.GetLanguage("JavaInstallCheck") + "...",
                    "", false, _metroDialogOptions);
                MainWindow.ProcessUITasks();
            }
            switch (java.GetJavaStatus())
            {
                case JavaResults.Absent:
                    {
                        // If java is not installed, offer to download it
                        await checkingJavaDialog.CloseAsync();
                        if (await _win.ShowMessageAsync(Program.Translations.GetLanguage("JavaNotFoundTitle"),
                            Program.Translations.GetLanguage("JavaNotFoundMessage"),
                            MessageDialogStyle.AffirmativeAndNegative, _metroDialogOptions) == MessageDialogResult.Affirmative)
                        {
                            await java.InstallJava();
                        }
                        return;
                    }
                case JavaResults.Outdated:
                    {
                        // If java is outdated, offer to upgrade it
                        await checkingJavaDialog.CloseAsync();
                        if (await _win.ShowMessageAsync(Program.Translations.GetLanguage("JavaOutdatedTitle"),
                             Program.Translations.GetLanguage("JavaOutdatedMessage"),
                             MessageDialogStyle.AffirmativeAndNegative, _metroDialogOptions) == MessageDialogResult.Affirmative)
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

            // Pick file for decompiling
            var ofd = new OpenFileDialog
            {
                Filter = "Sourcepawn Plugins (*.smx)|*.smx",
                Title = Program.Translations.GetLanguage("ChDecomp")
            };
            var result = ofd.ShowDialog();

            if (result.Value && !string.IsNullOrWhiteSpace(ofd.FileName))
            {
                var fInfo = new FileInfo(ofd.FileName);
                if (fInfo.Exists)
                {
                    ProgressDialogController task = null;
                    if (_win != null)
                    {
                        task = await _win.ShowProgressAsync(Program.Translations.GetLanguage("Decompiling") + "...",
                            fInfo.FullName, false, _metroDialogOptions);
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
                        await _win.ShowMessageAsync($"{fInfo.Name} {Program.Translations.GetLanguage("FailedToDecompile")}",
                            $"{ex.Message}", MessageDialogStyle.Affirmative,
                        _metroDialogOptions);
                    }

                    // Load the decompiled file to SPCode
                    _win.TryLoadSourceFile(destFile, true, false, true);
                    if (task != null)
                    {
                        await task.CloseAsync();
                    }
                }
            }
        }
    }
}
