using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using SPCode.Interop;
using SPCode.Utils;
using static SPCode.Interop.TranslationProvider;

namespace SPCode.UI
{
    public partial class MainWindow
    {
        private List<string> ScriptsCompiled = new();

        private readonly List<string> CompiledFileNames = new();
        private readonly List<string> CompiledFiles = new();
        private readonly List<string> NonUploadedFiles = new();
        private readonly List<ErrorDataGridRow> CurrentErrors = new();
        private readonly List<ErrorDataGridRow> CurrentWarnings = new();

        public int TotalErrors;
        public int TotalWarnings;

        private bool InCompiling;
        private Thread ServerCheckThread;

        private ProgressDialogController ProgressTask;

        private bool ServerIsRunning;
        private Process ServerProcess;

        private string CurrentErrorString;

        private static readonly Encoding _ansi = Encoding.GetEncoding(1251);

        private static readonly RegexOptions _regexOptions = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline;
        private readonly Regex _errorFilterRegex = new(Constants.ErrorFilterRegex, _regexOptions);

        /// <summary>
        /// Compiles the specified scripts.
        /// </summary>
        /// <param name="compileAll"></param>
        private async void Compile_SPScripts(bool compileAll = true)
        {
            // Checks if the program is compiling to avoid doing it again, and checks if the editor is from the templates window
            var ee = GetCurrentEditorElement();
            if (ee == null || InCompiling || ee.IsTemplateEditor)
            {
                return;
            }

            // Saves all editors, sets InCompiling flag, clears all fields
            Command_SaveAll();
            InCompiling = true;
            CompiledFiles.Clear();
            CompiledFileNames.Clear();
            NonUploadedFiles.Clear();
            CurrentErrors.Clear();
            CurrentWarnings.Clear();

            // Grabs current config
            var currentConfig = Program.Configs[Program.SelectedConfig];

            // Creates flags
            FileInfo spCompInfo = null;
            var SpCompFound = false;
            var PressedEscape = false;
            var dontCreateFile = false;
            TotalErrors = 0;
            TotalWarnings = 0;
            CurrentErrorString = string.Empty;

            // Searches for the spcomp.exe compiler
            foreach (var dir in currentConfig.SMDirectories)
            {
                spCompInfo = new FileInfo(Path.Combine(dir, Constants.SPCompiler));
                if (spCompInfo.Exists)
                {
                    SpCompFound = true;
                    break;
                }
            }

            if (!SpCompFound)
            {
                LoggingControl.LogAction($"No compiler found, aborting.");
                await this.ShowMessageAsync(Translate("Error"),
                    Translate("SPCompNotFound"), MessageDialogStyle.Affirmative,
                    MetroDialogOptions);
                InCompiling = false;
                return;
            }

            // If the compiler was found, it starts adding to a list all of the files to compile
            ScriptsCompiled.Clear();
            if (compileAll)
            {
                if (!EditorReferences.Any())
                {
                    InCompiling = false;
                    return;
                }

                foreach (var editor in EditorReferences)
                {
                    var compileBoxIsChecked = editor.CompileBox.IsChecked;
                    if (compileBoxIsChecked != null && compileBoxIsChecked.Value)
                    {
                        ScriptsCompiled.Add(editor.FullFilePath);
                    }
                    else
                    {
                        LoggingControl.LogAction($"{new FileInfo(editor.FullFilePath).Name} (omitted)");
                    }
                }
            }
            else
            {
                if (ee == null)
                {
                    InCompiling = false;
                    return;
                }

                if (ee.FullFilePath.EndsWith(".sp"))
                {
                    ScriptsCompiled.Add(ee.FullFilePath);
                }
            }

            var compileCount = ScriptsCompiled.Count;
            if (compileCount > 0)
            {
                // Shows the 'Compiling...' window
                ErrorResultGrid.Items.Clear();

                ProgressTask = await this.ShowProgressAsync(Translate("Compiling"), "",
                    false, MetroDialogOptions);
                ProgressTask.SetProgress(0.0);

                var stringOutput = new StringBuilder();

                var compiledSuccess = 0;

                // Loops through all files to compile
                for (var i = 0; i < compileCount; ++i)
                {
                    if (!InCompiling) //pressed escape
                    {
                        PressedEscape = true;
                        break;
                    }

                    var file = ScriptsCompiled[i];
                    ProgressTask.SetMessage($"{file} ({i}/{compileCount}) ");
                    ProcessUITasks();
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.Exists)
                    {
                        dontCreateFile = ee.DontCreateFileBox.IsChecked.HasValue && ee.DontCreateFileBox.IsChecked.Value;
                        string outFile;
                        string destinationFileName;
                        if (dontCreateFile)
                        {
                            outFile = "NUL";
                            destinationFileName = string.Empty;
                        }
                        else
                        {
                            destinationFileName = ShortenScriptFileName(fileInfo.Name) + ".smx";
                            outFile = Path.Combine(fileInfo.DirectoryName, destinationFileName);
                            if (File.Exists(outFile))
                            {
                                File.Delete(outFile);
                            }
                            ExecuteCommandLine(currentConfig.PreCmd, fileInfo.DirectoryName, currentConfig.CopyDirectory,
                            fileInfo.FullName, fileInfo.Name, outFile, destinationFileName);
                        }

                        var includeDirectories = new StringBuilder();
                        foreach (var dir in currentConfig.SMDirectories)
                        {
                            includeDirectories.Append(" -i=\"" + dir + "\"");
                        }

                        var includeStr = includeDirectories.ToString();

                        var process = new Process();
                        var startInfo = new ProcessStartInfo
                        {
                            WorkingDirectory = fileInfo.DirectoryName ?? throw new NullReferenceException(),
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true,
                            FileName = spCompInfo.FullName,
                            Arguments = $"\"{fileInfo.FullName}\" -o=\"{outFile}\" {includeStr} -O={currentConfig.OptimizeLevel} -v={currentConfig.VerboseLevel}"
                        };

                        var sb = new StringBuilder();
                        process.StartInfo = startInfo;
                        ProgressTask.SetProgress((i + 1 - 0.5d) / compileCount);

                        ProcessUITasks();

                        try
                        {
                            process.Start();
                            sb.Append(process.StandardOutput.ReadToEnd());
                            process.WaitForExit();
                        }
                        catch (Exception)
                        {
                            await ProgressTask.CloseAsync();
                            await this.ShowMessageAsync(Translate("SPCompNotStarted"),
                                Translate("Error"), MessageDialogStyle.Affirmative,
                                MetroDialogOptions);
                            InCompiling = false;
                            return;
                        }

                        switch (process.ExitCode)
                        {
                            // Successful compilation (could have warnings)
                            case 0:
                            {
                                var matches = _errorFilterRegex.Matches(sb.ToString());
                                foreach (Match match in matches)
                                {
                                    TotalWarnings++;
                                    var item = new ErrorDataGridRow(match);
                                    if (!HideWarnings)
                                    {
                                        ErrorResultGrid.Items.Add(item);
                                    }
                                    CurrentWarnings.Add(item);
                                }
                                LoggingControl.LogAction($"{fileInfo.Name}{(TotalWarnings > 0 ? $" ({TotalWarnings} warnings)" : "")}");
                                compiledSuccess++;
                                break;
                            }

                            // Failed compilation
                            case 1:
                            {
                                LoggingControl.LogAction(fileInfo.Name + " (error)");
                                var matches = _errorFilterRegex.Matches(sb.ToString());
                                foreach (Match match in matches)
                                {
                                    var item = new ErrorDataGridRow(match);
                                    if (item.IsError)
                                    {
                                        TotalErrors++;
                                        if (!HideErrors)
                                        {
                                            ErrorResultGrid.Items.Add(item);
                                        }
                                        CurrentErrors.Add(item);
                                    }
                                    if (item.IsWarning)
                                    {
                                        TotalWarnings++;
                                        if (!HideWarnings)
                                        {
                                            ErrorResultGrid.Items.Add(item);
                                        }
                                        CurrentWarnings.Add(item);
                                    }
                                }
                                break;
                            }

                            // Compiler crash
                            default:
                            {
                                await ProgressTask.CloseAsync();
                                await this.ShowMessageAsync(Translate("Error"),
                                    "The SourcePawn compiler has crashed.\n" +
                                    "Try again, or file an issue at the SourcePawn GitHub repository describing your steps that led to this instance in detail.\n" +
                                    $"Exit code: {process.ExitCode:X}", MessageDialogStyle.Affirmative,
                                    MetroDialogOptions);
                                LoggingControl.LogAction($"Compiler crash detected, file: {fileInfo.Name}", 2);
                                InCompiling = false;
                                return;
                            }
                        }

                        if (!dontCreateFile && File.Exists(outFile))
                        {
                            CompiledFiles.Add(outFile);
                            NonUploadedFiles.Add(outFile);
                            CompiledFileNames.Add(destinationFileName);
                        }

                        if (!dontCreateFile)
                        {
                            ExecuteCommandLine(currentConfig.PostCmd, fileInfo.DirectoryName,
                                currentConfig.CopyDirectory, fileInfo.FullName, fileInfo.Name, outFile, destinationFileName);
                        }

                        ProgressTask.SetProgress((double)(i + 1) / compileCount);
                        ProcessUITasks();
                    }
                }

                if (compiledSuccess > 0)
                {
                    LoggingControl.LogAction($"Compiled {compiledSuccess} {(compiledSuccess > 1 ? "plugins" : "plugin")}.", 2);
                }

                Status_ErrorText.Text = (TotalErrors == 1 ? string.Format(Translate("status_error"), "1") : string.Format(Translate("status_errors"), TotalErrors)).ToLowerInvariant();
                Status_WarningText.Text = (TotalWarnings == 1 ? string.Format(Translate("status_warning"), "1") : string.Format(Translate("status_warnings"), TotalWarnings)).ToLowerInvariant();

                if (!PressedEscape && compiledSuccess > 0)
                {
                    ProgressTask.SetProgress(1.0);
                    if (currentConfig.AutoCopy && !dontCreateFile)
                    {
                        ProgressTask.SetTitle(Translate("CopyingFiles") + "...");
                        ProgressTask.SetIndeterminate();
                        await Task.Run(() => Copy_Plugins());
                        ProgressTask.SetProgress(1.0);
                    }

                    if (currentConfig.AutoUpload && !dontCreateFile)
                    {
                        ProgressTask.SetTitle(Translate("FTPUploading") + "...");
                        ProgressTask.SetIndeterminate();
                        await Task.Run(FTPUpload_Plugins);
                        ProgressTask.SetProgress(1.0);
                    }

                    if (currentConfig.AutoRCON && !dontCreateFile)
                    {
                        ProgressTask.SetTitle(Translate("RCONCommand") + "...");
                        ProgressTask.SetIndeterminate();
                        await Task.Run(Server_Query);
                        ProgressTask.SetProgress(1.0);
                    }

                }

                if (CompileOutputRow.Height.Value < 11.0)
                {
                    CompileOutputRow.Height = new GridLength(200.0);
                }

                await ProgressTask.CloseAsync();
            }
            InCompiling = false;
        }

        /// <summary>
        /// Copies the compiled plugins to the specified destination.
        /// </summary>
        private void Copy_Plugins()
        {
            var output = new List<string>();
            if (CompiledFiles.Count <= 0)
            {
                LoggingControl.LogAction("No plugins found to copy.", 2);
                return;
            }
            var copyCount = 0;
            var c = Program.Configs[Program.SelectedConfig];
            if (string.IsNullOrWhiteSpace(c.CopyDirectory))
            {
                output.Add($"Copy directory is empty.");
                goto Dispatcher;
            }
            if (!Directory.Exists(c.CopyDirectory))
            {
                output.Add("The specified Copy Directory was not found.");
                goto Dispatcher;
            }
            output.Add($"Copying plugin(s)...");
            NonUploadedFiles.Clear();
            var stringOutput = new StringBuilder();
            foreach (var file in CompiledFiles)
            {
                var destFile = new FileInfo(file);
                try
                {
                    if (destFile.Exists)
                    {
                        var destinationFileName = destFile.Name;
                        var copyFileDestination = Path.Combine(c.CopyDirectory, destinationFileName);
                        File.Copy(file, copyFileDestination, true);
                        NonUploadedFiles.Add(copyFileDestination);
                        output.Add($"{Translate("Copied")}: {copyFileDestination}");
                        ++copyCount;
                        if (c.DeleteAfterCopy)
                        {
                            File.Delete(file);
                            output.Add($"{Translate("Deleted")}: {copyFileDestination}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    output.Add($"{Translate("FailCopy")}: {destFile.Name}");
                    output.Add(ex.Message);
                }
            }

            if (copyCount == 0)
            {
                output.Add($"{Translate("NoFilesCopy")}");
            }

        Dispatcher:

            Dispatcher.Invoke(() =>
            {
                output.ForEach(x => LoggingControl.LogAction(x));
                if (CompileOutputRow.Height.Value < 11.0)
                {
                    CompileOutputRow.Height = new GridLength(200.0);
                }
            });
        }

        /// <summary>
        /// Uploads the compiled plugins via FTP to the specified destination.
        /// </summary>
        private void FTPUpload_Plugins()
        {
            var output = new List<string>();
            if (NonUploadedFiles.Count <= 0)
            {
                LoggingControl.LogAction("No plugins found to upload.", 2);
                return;
            }

            var c = Program.Configs[Program.SelectedConfig];
            if (string.IsNullOrWhiteSpace(c.FTPHost) || string.IsNullOrWhiteSpace(c.FTPUser))
            {
                output.Add("FTP Host or User fields are empty.");
                goto Dispatcher;
            }

            output.Add("Uploading plugin(s)...");

            try
            {
                var ftp = new FTP(c.FTPHost, c.FTPUser, c.FTPPassword);
                foreach (var file in NonUploadedFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.Exists)
                    {
                        string uploadDir;
                        if (string.IsNullOrWhiteSpace(c.FTPDir))
                        {
                            uploadDir = fileInfo.Name;
                        }
                        else
                        {
                            uploadDir = c.FTPDir.TrimEnd('/') + "/" + fileInfo.Name;
                        }

                        try
                        {
                            ftp.Upload(uploadDir, file);
                            output.Add($"{Translate("Uploaded")}: {fileInfo.Name}");
                        }
                        catch (Exception e)
                        {
                            output.Add(string.Format(Translate("ErrorUploadFile"),
                                fileInfo.Name, uploadDir));
                            output.Add($"{Translate("Details")}: {e.Message}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                output.Add(Translate("ErrorUpload"));
                output.Add($"{Translate("Details")}: " + e.Message);
            }

        Dispatcher:

            Dispatcher.Invoke(() =>
            {
                output.ForEach(x => LoggingControl.LogAction(x));
                if (CompileOutputRow.Height.Value < 11.0)
                {
                    CompileOutputRow.Height = new GridLength(200.0);
                }
            });
        }

        /// <summary>
        /// Starts the server with the specified parameters.
        /// </summary>
        private void Server_Start()
        {
            if (ServerIsRunning)
            {
                LoggingControl.LogAction("The server is already running!", 2);
                return;
            }
            var c = Program.Configs[Program.SelectedConfig];
            var serverOptionsPath = c.ServerFile;
            if (string.IsNullOrWhiteSpace(serverOptionsPath))
            {
                LoggingControl.LogAction("No executable specified to start server.", 2);
                return;
            }

            var serverExec = new FileInfo(serverOptionsPath);
            if (!serverExec.Exists)
            {
                LoggingControl.LogAction("The specified server executable file doesn't exist.", 2);
                return;
            }

            LoggingControl.LogAction("Starting server...");
            try
            {
                ServerProcess = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = true,
                        FileName = serverExec.FullName,
                        WorkingDirectory = serverExec.DirectoryName ?? throw new NullReferenceException(),
                        Arguments = c.ServerArgs
                    }
                };
                ServerCheckThread = new Thread(ProcessCheckWorker);
                ServerCheckThread.Start();
            }
            catch (Exception)
            {
                ServerProcess?.Dispose();
            }
        }

        private void ProcessCheckWorker()
        {
            try
            {
                ServerProcess.Start();
                ServerIsRunning = true;
                Program.MainWindow.Dispatcher?.Invoke(() =>
                {
                    EnableServerAnim.Begin();
                    UpdateWindowTitle();
                    LoggingControl.LogAction("Server started.", 2);
                });
                ServerProcess.WaitForExit();
                ServerProcess.Dispose();
                ServerIsRunning = false;
                Program.MainWindow.Dispatcher?.Invoke(() =>
                {
                    if (Program.MainWindow.IsLoaded)
                    {
                        DisableServerAnim.Begin();
                        UpdateWindowTitle();
                        LoggingControl.LogAction("Server stopped.", 2);
                    }
                });
            }
            catch (Exception)
            {
                return;
            }
        }

        private string ShortenScriptFileName(string fileName)
        {
            if (fileName.EndsWith(".sp", StringComparison.InvariantCultureIgnoreCase))
            {
                return fileName.Substring(0, fileName.Length - 3);
            }

            return fileName;
        }

        /// <summary>
        /// Executes the commands from the pre and post compile commands boxes.
        /// </summary>
        private string ExecuteCommandLine(string code, string directory, string copyDir, string scriptFile,
            string scriptName, string pluginFile, string pluginName)
        {
            code = ReplaceCMDVariables(code, directory, copyDir, scriptFile, scriptName, pluginFile, pluginName);
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var batchFile = new FileInfo(@$"{PathsHelper.TempDirectory}\{Environment.TickCount}_{(uint)code.GetHashCode() ^ (uint)directory.GetHashCode()}_temp.bat").FullName;
            File.WriteAllText(batchFile, code);
            string result;
            using (var process = new Process())
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.WorkingDirectory = directory;
                process.StartInfo.Arguments = "/c \"" + batchFile + "\"";
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();
                process.WaitForExit();
                using var reader = process.StandardOutput;
                result = reader.ReadToEnd();
            }

            File.Delete(batchFile);
            return result;
        }

        /// <summary>
        /// Replaces the placeholders from the pre and post compile commands boxes with the corresponding content.
        /// </summary>
        private string ReplaceCMDVariables(string CMD, string scriptDir, string copyDir, string scriptFile,
            string scriptName, string pluginFile, string pluginName)
        {
            CMD = CMD.Replace("{editordir}", Environment.CurrentDirectory.Trim('\\'));
            CMD = CMD.Replace("{scriptdir}", scriptDir);
            CMD = CMD.Replace("{copydir}", copyDir);
            CMD = CMD.Replace("{scriptfile}", scriptFile);
            CMD = CMD.Replace("{scriptname}", scriptName);
            CMD = CMD.Replace("{pluginfile}", pluginFile);
            CMD = CMD.Replace("{pluginname}", pluginName);
            return CMD;
        }
    }
}