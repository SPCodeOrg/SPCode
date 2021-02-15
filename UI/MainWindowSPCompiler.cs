using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using SPCode.Utils;

namespace SPCode.UI
{
    public partial class MainWindow
    {
        private readonly List<string> compiledFileNames = new List<string>();
        private readonly List<string> compiledFiles = new List<string>();
        private readonly List<string> nonUploadedFiles = new List<string>();

        private bool InCompiling;
        private Thread ServerCheckThread;

        private bool ServerIsRunning;
        private Process ServerProcess;

        private async void Compile_SPScripts(bool compileAll = true)
        {
            // Checks if the program is compiling to avoid doing it again
            if (InCompiling)
            {
                return;
            }

            // Saves all editors, sets InCompiling flag, clears all fields
            Command_SaveAll();
            InCompiling = true;
            compiledFiles.Clear();
            compiledFileNames.Clear();
            nonUploadedFiles.Clear();

            // Grabs current config
            var c = Program.Configs[Program.SelectedConfig];

            // Creates all flags and wrappers
            FileInfo spCompInfo = null;
            var SpCompFound = false;
            var PressedEscape = false;

            // Searches for the spcomp.exe compiler
            foreach (var dir in c.SMDirectories)
            {
                spCompInfo = new FileInfo(Path.Combine(dir, "spcomp.exe"));
                if (spCompInfo.Exists)
                {
                    SpCompFound = true;
                    break;
                }
            }

            if (SpCompFound)
            {
                // If the compiler was found, it starts adding to a list all of the files to compile
                var filesToCompile = new List<string>();
                if (compileAll)
                {
                    var editors = GetAllEditorElements();
                    if (editors == null)
                    {
                        InCompiling = false;
                        return;
                    }

                    foreach (var t in editors)
                    {
                        var compileBoxIsChecked = t.CompileBox.IsChecked;
                        if (compileBoxIsChecked != null && compileBoxIsChecked.Value)
                        {
                            filesToCompile.Add(t.FullFilePath);
                        }
                    }
                }
                else
                {
                    var ee = GetCurrentEditorElement();
                    if (ee == null)
                    {
                        InCompiling = false;
                        return;
                    }

                    /*
                    ** I've struggled a bit here. Should i check, if the CompileBox is checked 
                    ** and only compile if it's checked or should it be ignored and compiled anyway?
                    ** I decided, to compile anyway but give me feedback/opinions.
                    */
                    if (ee.FullFilePath.EndsWith(".sp"))
                    {
                        filesToCompile.Add(ee.FullFilePath);
                   }
                }

                var compileCount = filesToCompile.Count;
                if (compileCount > 0)
                {
                    // Shows the 'Compiling...' window
                    ErrorResultGrid.Items.Clear();
                    var progressTask = await this.ShowProgressAsync(Program.Translations.GetLanguage("Compiling"), "",
                        false, MetroDialogOptions);
                    progressTask.SetProgress(0.0);
                    var stringOutput = new StringBuilder();
                    var errorFilterRegex =
                        new Regex(
                            @"^(?<File>.+?)\((?<Line>[0-9]+(\s*--\s*[0-9]+)?)\)\s*:\s*(?<Type>[a-zA-Z]+\s+([a-zA-Z]+\s+)?[0-9]+)\s*:(?<Details>.+)",
                            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline);

                    // Loops through all files to compile
                    for (var i = 0; i < compileCount; ++i)
                    {
                        if (!InCompiling) //pressed escape
                        {
                            PressedEscape = true;
                            break;
                        }

                        var file = filesToCompile[i];
                        progressTask.SetMessage($"{file} ({i}/{compileCount}) ");
                        ProcessUITasks();
                        var fileInfo = new FileInfo(file);
                        stringOutput.AppendLine(fileInfo.Name);
                        if (fileInfo.Exists)
                        {
                            var process = new Process();
                            process.StartInfo.WorkingDirectory =
                                fileInfo.DirectoryName ?? throw new NullReferenceException();
                            process.StartInfo.UseShellExecute = true;
                            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                            process.StartInfo.CreateNoWindow = true;
                            process.StartInfo.FileName = spCompInfo.FullName;
                            var destinationFileName = ShortenScriptFileName(fileInfo.Name) + ".smx";
                            var outFile = Path.Combine(fileInfo.DirectoryName, destinationFileName);
                            if (File.Exists(outFile))
                            {
                                File.Delete(outFile);
                            }

                            var errorFile = $@"{fileInfo.DirectoryName}\error_{Environment.TickCount}_{file.GetHashCode():X}_{i}.txt";
                            if (File.Exists(errorFile))
                            {
                                File.Delete(errorFile);
                            }

                            var includeDirectories = new StringBuilder();
                            foreach (var dir in c.SMDirectories)
                            {
                                includeDirectories.Append(" -i=\"" + dir + "\"");
                            }

                            var includeStr = includeDirectories.ToString();

                            process.StartInfo.Arguments =
                                "\"" + fileInfo.FullName + "\" -o=\"" + outFile + "\" -e=\"" + errorFile + "\"" +
                                includeStr + " -O=" + c.OptimizeLevel + " -v=" + c.VerboseLevel;
                            progressTask.SetProgress((i + 1 - 0.5d) / compileCount);
                            var execResult = ExecuteCommandLine(c.PreCmd, fileInfo.DirectoryName, c.CopyDirectory,
                                fileInfo.FullName, fileInfo.Name, outFile, destinationFileName);
                            if (!string.IsNullOrWhiteSpace(execResult))
                            {
                                stringOutput.AppendLine(execResult.Trim('\n', '\r'));
                            }

                            ProcessUITasks();
                            try
                            {
                                process.Start();
                                process.WaitForExit();
                            }
                            catch (Exception)
                            {
                                InCompiling = false;
                            }

                            if (!InCompiling) //cannot await in catch
                            {
                                await progressTask.CloseAsync();
                                await this.ShowMessageAsync(Program.Translations.GetLanguage("SPCompNotStarted"),
                                    Program.Translations.GetLanguage("Error"), MessageDialogStyle.Affirmative,
                                    MetroDialogOptions);
                                return;
                            }

                            if (File.Exists(errorFile))
                            {
                                var errorStr = File.ReadAllText(errorFile);
                                stringOutput.AppendLine(errorStr.Trim('\n', '\r'));
                                var mc = errorFilterRegex.Matches(errorStr);
                                for (var j = 0; j < mc.Count; ++j)
                                {
                                    ErrorResultGrid.Items.Add(new ErrorDataGridRow
                                    {
                                        File = mc[j].Groups["File"].Value.Trim(),
                                        Line = mc[j].Groups["Line"].Value.Trim(),
                                        Type = mc[j].Groups["Type"].Value.Trim(),
                                        Details = mc[j].Groups["Details"].Value.Trim()
                                    });
                                }

                                File.Delete(errorFile);
                            }

                            stringOutput.AppendLine(Program.Translations.GetLanguage("Done"));
                            if (File.Exists(outFile))
                            {
                                compiledFiles.Add(outFile);
                                nonUploadedFiles.Add(outFile);
                                compiledFileNames.Add(destinationFileName);
                            }

                            var execResult_Post = ExecuteCommandLine(c.PostCmd, fileInfo.DirectoryName,
                                c.CopyDirectory, fileInfo.FullName, fileInfo.Name, outFile, destinationFileName);
                            if (!string.IsNullOrWhiteSpace(execResult_Post))
                            {
                                stringOutput.AppendLine(execResult_Post.Trim('\n', '\r'));
                            }

                            stringOutput.AppendLine();
                            progressTask.SetProgress((double)(i + 1) / compileCount);
                            ProcessUITasks();
                        }
                    }

                    if (!PressedEscape)
                    {
                        progressTask.SetProgress(1.0);
                        CompileOutput.Text = stringOutput.ToString();
                        if (c.AutoCopy)
                        {
                            progressTask.SetTitle(Program.Translations.GetLanguage("CopyingFiles"));
                            progressTask.SetIndeterminate();
                            await Task.Run(() => Copy_Plugins(true));
                            progressTask.SetProgress(1.0);
                        }

                        if (c.AutoUpload)
                        {
                            progressTask.SetTitle(Program.Translations.GetLanguage("FTPUploading"));
                            progressTask.SetIndeterminate();
                            await Task.Run(FTPUpload_Plugins);
                            progressTask.SetProgress(1.0);
                        }

                        if (c.AutoRCON)
                        {
                            progressTask.SetTitle(Program.Translations.GetLanguage("RCONCommand"));
                            progressTask.SetIndeterminate();
                            await Task.Run(Server_Query);
                            progressTask.SetProgress(1.0);
                        }

                        if (CompileOutputRow.Height.Value < 11.0)
                        {
                            CompileOutputRow.Height = new GridLength(200.0);
                        }
                    }

                    await progressTask.CloseAsync();
                }
            }
            else
            {
                await this.ShowMessageAsync(Program.Translations.GetLanguage("Error"),
                    Program.Translations.GetLanguage("SPCompNotFound"), MessageDialogStyle.Affirmative,
                    MetroDialogOptions);
            }

            InCompiling = false;
        }

        private void Copy_Plugins(bool OvertakeOutString = false)
        {
            if (compiledFiles.Count > 0)
            {
                var copyCount = 0;
                var c = Program.Configs[Program.SelectedConfig];
                if (!string.IsNullOrWhiteSpace(c.CopyDirectory))
                {
                    nonUploadedFiles.Clear();

                    var stringOutput = new StringBuilder();
                    foreach (var file in compiledFiles)
                    {
                        try
                        {
                            var destFile = new FileInfo(file);
                            if (destFile.Exists)
                            {
                                var destinationFileName = destFile.Name;
                                var copyFileDestination = Path.Combine(c.CopyDirectory, destinationFileName);
                                File.Copy(file, copyFileDestination, true);
                                nonUploadedFiles.Add(copyFileDestination);
                                stringOutput.AppendLine($"{Program.Translations.GetLanguage("Copied")}: " + file);
                                ++copyCount;
                                if (c.DeleteAfterCopy)
                                {
                                    File.Delete(file);
                                    stringOutput.AppendLine($"{Program.Translations.GetLanguage("Deleted")}: " + file);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            stringOutput.AppendLine($"{Program.Translations.GetLanguage("FailCopy")}: " + file);
                        }
                    }

                    if (copyCount == 0)
                    {
                        stringOutput.AppendLine(Program.Translations.GetLanguage("NoFilesCopy"));
                    }

                    Dispatcher.Invoke(() =>
                    {
                        if (OvertakeOutString)
                        {
                            CompileOutput.AppendText(stringOutput.ToString());
                        }
                        else
                        {
                            CompileOutput.Text = stringOutput.ToString();
                        }

                        if (CompileOutputRow.Height.Value < 11.0)
                        {
                            CompileOutputRow.Height = new GridLength(200.0);
                        }
                    });
                }
            }
        }

        private void FTPUpload_Plugins()
        {
            if (nonUploadedFiles.Count <= 0)
            {
                return;
            }

            var c = Program.Configs[Program.SelectedConfig];
            if (string.IsNullOrWhiteSpace(c.FTPHost) || string.IsNullOrWhiteSpace(c.FTPUser))
            {
                return;
            }

            var stringOutput = new StringBuilder();
            try
            {
                var ftp = new FTP(c.FTPHost, c.FTPUser, c.FTPPassword);
                foreach (var file in nonUploadedFiles)
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
                            stringOutput.AppendLine($"{Program.Translations.GetLanguage("Uploaded")}: " + file);
                        }
                        catch (Exception e)
                        {
                            stringOutput.AppendLine(string.Format(Program.Translations.GetLanguage("ErrorUploadFile"),
                                file, uploadDir));
                            stringOutput.AppendLine($"{Program.Translations.GetLanguage("Details")}: " + e.Message);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                stringOutput.AppendLine(Program.Translations.GetLanguage("ErrorUpload"));
                stringOutput.AppendLine($"{Program.Translations.GetLanguage("Details")}: " + e.Message);
            }

            stringOutput.AppendLine(Program.Translations.GetLanguage("Done"));
            Dispatcher.Invoke(() =>
            {
                CompileOutput.Text = stringOutput.ToString();
                if (CompileOutputRow.Height.Value < 11.0)
                {
                    CompileOutputRow.Height = new GridLength(200.0);
                }
            });
        }

        private void Server_Start()
        {
            if (ServerIsRunning)
            {
                return;
            }

            var c = Program.Configs[Program.SelectedConfig];
            var serverOptionsPath = c.ServerFile;
            if (string.IsNullOrWhiteSpace(serverOptionsPath))
            {
                return;
            }

            var serverExec = new FileInfo(serverOptionsPath);
            if (!serverExec.Exists)
            {
                return;
            }

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
            }
            catch (Exception)
            {
                return;
            }

            ServerIsRunning = true;
            Program.MainWindow.Dispatcher?.Invoke(() =>
            {
                EnableServerAnim.Begin();
                UpdateWindowTitle();
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
                }
            });
        }


        private string ShortenScriptFileName(string fileName)
        {
            if (fileName.EndsWith(".sp", StringComparison.InvariantCultureIgnoreCase))
            {
                return fileName.Substring(0, fileName.Length - 3);
            }

            return fileName;
        }

        private string ExecuteCommandLine(string code, string directory, string copyDir, string scriptFile,
            string scriptName, string pluginFile, string pluginName)
        {
            code = ReplaceCMDVariables(code, directory, copyDir, scriptFile, scriptName, pluginFile, pluginName);
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var batchFile = new FileInfo(@$"{Paths.GetTempDirectory()}\{Environment.TickCount}_{(uint)code.GetHashCode() ^ (uint)directory.GetHashCode()}_temp.bat").FullName;
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

    public class ErrorDataGridRow
    {
        public string File { set; get; }
        public string Line { set; get; }
        public string Type { set; get; }
        public string Details { set; get; }
    }
}