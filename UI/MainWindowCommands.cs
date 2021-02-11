using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using SPCode.UI.Components;
using SPCode.UI.Windows;
using SPCode.Utils;
using SPCode.Utils.SPSyntaxTidy;
using static SPCode.Utils.JavaVersionChecker;

namespace SPCode.UI
{
    public partial class MainWindow
    {
        public EditorElement GetCurrentEditorElement()
        {
            EditorElement outElement = null;
            if (DockingPane.SelectedContent?.Content != null)
            {
                var possElement = DockingManager.ActiveContent;
                if (possElement is EditorElement element)
                {
                    outElement = element;
                }
            }

            return outElement;
        }

        public EditorElement[] GetAllEditorElements()
        {
            return EditorsReferences.Count < 1 ? null : EditorsReferences.ToArray();
        }

        private void Command_New()
        {
            var nfWindow = new NewFileWindow { Owner = this, ShowInTaskbar = false };
            nfWindow.ShowDialog();
        }

        private void Command_Open()
        {
            var ofd = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter =
                    @"Sourcepawn Files (*.sp *.inc)|*.sp;*.inc|Sourcemod Plugins (*.smx)|*.smx|All Files (*.*)|*.*",
                Multiselect = true,
                Title = Program.Translations.GetLanguage("OpenNewFile")
            };
            var result = ofd.ShowDialog(this);
            if (result.Value)
            {
                var AnyFileLoaded = false;
                if (ofd.FileNames.Length > 0)
                {
                    for (var i = 0; i < ofd.FileNames.Length; ++i)
                    {
                        AnyFileLoaded |= TryLoadSourceFile(ofd.FileNames[i], i == 0, true, i == 0);
                    }

                    if (!AnyFileLoaded)
                    {
                        MetroDialogOptions.ColorScheme = MetroDialogColorScheme.Theme;
                        this.ShowMessageAsync(Program.Translations.GetLanguage("NoFileOpened"),
                            Program.Translations.GetLanguage("NoFileOpenedCap"), MessageDialogStyle.Affirmative,
                            MetroDialogOptions);
                    }
                }
            }

            Activate();
        }

        private void Command_Save()
        {
            var ee = GetCurrentEditorElement();
            if (ee != null)
            {
                ee.Save(true);
                BlendOverEffect.Begin();
            }
        }

        private void Command_SaveAs()
        {
            var ee = GetCurrentEditorElement();
            if (ee != null)
            {
                var sfd = new SaveFileDialog
                {
                    AddExtension = true,
                    Filter = @"Sourcepawn Files (*.sp *.inc)|*.sp;*.inc|All Files (*.*)|*.*",
                    OverwritePrompt = true,
                    Title = Program.Translations.GetLanguage("SaveFileAs"),
                    FileName = ee.Parent.Title.Trim('*')
                };
                var result = sfd.ShowDialog(this);
                if (result.Value && !string.IsNullOrWhiteSpace(sfd.FileName))
                {
                    ee.FullFilePath = sfd.FileName;
                    ee.Save(true);
                    BlendOverEffect.Begin();
                }
            }
        }

        private void Command_SaveAll()
        {
            var editors = GetAllEditorElements();
            if (editors == null)
            {
                return;
            }

            if (editors.Length > 0)
            {
                foreach (var editor in editors)
                {
                    editor.Save();
                }

                BlendOverEffect.Begin();
            }
        }

        private void Command_Close()
        {
            var ee = GetCurrentEditorElement();
            if (ee == null)
            {
                return;
            }

            DockingPane.RemoveChild(ee.Parent);
            ee.Close();
        }

        private async void Command_CloseAll()
        {
            var editors = GetAllEditorElements();
            if (editors == null)
            {
                return;
            }

            if (editors.Length > 0)
            {
                var UnsavedEditorsExisting = false;
                foreach (var editor in editors)
                {
                    UnsavedEditorsExisting |= editor.NeedsSave;
                }

                var ForceSave = false;
                if (UnsavedEditorsExisting)
                {
                    var str = new StringBuilder();
                    for (var i = 0; i < editors.Length; ++i)
                    {
                        if (i == 0)
                        {
                            str.Append(editors[i].Parent.Title.Trim('*'));
                        }
                        else
                        {
                            str.AppendLine(editors[i].Parent.Title.Trim('*'));
                        }
                    }

                    var Result = await this.ShowMessageAsync(Program.Translations.GetLanguage("SaveFollow"),
                        str.ToString(), MessageDialogStyle.AffirmativeAndNegative, MetroDialogOptions);
                    if (Result == MessageDialogResult.Affirmative)
                    {
                        ForceSave = true;
                    }
                }

                foreach (var editor in editors)
                {
                    DockingPane.RemoveChild(editor.Parent);
                    editor.Close(ForceSave, ForceSave);
                }
            }
        }

        private void Command_Undo()
        {
            var ee = GetCurrentEditorElement();
            if (ee != null)
            {
                if (ee.editor.CanUndo)
                {
                    ee.editor.Undo();
                }
            }
        }

        private void Command_Redo()
        {
            var ee = GetCurrentEditorElement();
            if (ee != null)
            {
                if (ee.editor.CanRedo)
                {
                    ee.editor.Redo();
                }
            }
        }

        private void Command_Cut()
        {
            var ee = GetCurrentEditorElement();
            ee?.editor.Cut();
        }

        private void Command_Copy()
        {
            var ee = GetCurrentEditorElement();
            ee?.editor.Copy();
        }

        private void Command_Paste()
        {
            var ee = GetCurrentEditorElement();
            ee?.editor.Paste();
        }

        private void Command_FlushFoldingState(bool state)
        {
            var ee = GetCurrentEditorElement();
            if (ee?.foldingManager != null)
            {
                var foldings = ee.foldingManager.AllFoldings;
                foreach (var folding in foldings)
                {
                    folding.IsFolded = state;
                }
            }
        }

        private void Command_JumpTo()
        {
            var ee = GetCurrentEditorElement();
            ee?.ToggleJumpGrid();
        }

        private void Command_SelectAll()
        {
            var ee = GetCurrentEditorElement();
            ee?.editor.SelectAll();
        }

        private void Command_ToggleCommentLine()
        {
            var ee = GetCurrentEditorElement();
            ee?.ToggleCommentOnLine();
        }

        private void Command_TidyCode(bool All)
        {
            var editors = All ? GetAllEditorElements() : new[] { GetCurrentEditorElement() };
            if (editors == null)
            {
                return;
            }
            foreach (var ee in editors)
            {
                if (ee != null)
                {
                    int currentCaret = ee.editor.TextArea.Caret.Offset, numOfSpacesOrTabsBefore = 0;
                    var line = ee.editor.Document.GetLineByOffset(currentCaret);
                    var lineNumber = line.LineNumber;
                    // 0 - start | any other - middle | -1 - EOS
                    var curserLinePos = currentCaret == line.Offset ? 0 : currentCaret == line.EndOffset ? -1 : currentCaret - line.Offset;

                    if (curserLinePos > 0)
                    {
                        numOfSpacesOrTabsBefore = ee.editor.Document.GetText(line).Count(c => c == ' ' || c == '\t');
                    }

#if DEBUG
                    Debug.WriteLine($"Curser offset before format: {currentCaret}");

                    // Where is out curser?
                    if (currentCaret == line.Offset)
                    {
                        Debug.WriteLine("Curser is at the start of the line");
                    }
                    else if (currentCaret == line.EndOffset)
                    {
                        Debug.WriteLine("Curser is at the end of the line");
                    }
                    else
                    {
                        Debug.WriteLine("Curser is somewhere in the middle of the line");
                    }
#endif
                    // Formatting Start //
                    ee.editor.Document.BeginUpdate();
                    var source = ee.editor.Text;
                    ee.editor.Document.Replace(0, source.Length, SPSyntaxTidy.TidyUp(source));
                    ee.editor.Document.EndUpdate();
                    // Formatting End //

                    line = ee.editor.Document.GetLineByNumber(lineNumber);
                    var newCaretPos = line.Offset;
                    if (curserLinePos == -1)
                    {
                        newCaretPos += line.Length;
                    }
                    else if (curserLinePos != 0)
                    {
                        var numOfSpacesOrTabsAfter = ee.editor.Document.GetText(line).Count(c => c == ' ' || c == '\t');
                        newCaretPos += curserLinePos + (numOfSpacesOrTabsAfter - numOfSpacesOrTabsBefore);
#if DEBUG
                        Debug.WriteLine($"Curser offset after format: {newCaretPos}");
#endif
                    }
                    ee.editor.TextArea.Caret.Offset = newCaretPos;
                }
            }
        }

        private async void Command_Decompile(MainWindow win)
        {
            // First we check the java version of the user, and act accordingly
            JavaVersionChecker jvc = new JavaVersionChecker();
            switch (jvc.GetJavaStatus())
            {
                case JavaResults.Absent:
                    {
                        if (await this.ShowMessageAsync("Java was not found",
                            "Java could not be executed properly by SPCode. We suspect it's not properly installed, or not installed at all. " +
                            "Do you wish to download and install it now?",
                            MessageDialogStyle.AffirmativeAndNegative, MetroDialogOptions) == MessageDialogResult.Affirmative)
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = Environment.Is64BitOperatingSystem ? Constants.JavaDownloadSite64 : Constants.JavaDownloadSite32,
                                UseShellExecute = true
                            });
                        }
                        return;
                    }
                case JavaResults.Outdated:
                    {
                        if (await this.ShowMessageAsync("Java found is outdated",
                             "Lysis-Java requires Java 11 SDK or later to decompile plugins. We found an outdated version in your system. " +
                             "Do you wish to download and upgrade it now?",
                             MessageDialogStyle.AffirmativeAndNegative, MetroDialogOptions) == MessageDialogResult.Affirmative)
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = Environment.Is64BitOperatingSystem ? Constants.JavaDownloadSite64 : Constants.JavaDownloadSite32,
                                UseShellExecute = true
                            });
                        }
                        return;
                    }
                case JavaResults.Correct:
                    {
                        break;
                    }
            }

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
                    if (win != null)
                    {
                        task = await this.ShowProgressAsync(Program.Translations.GetLanguage("Decompiling"),
                            fInfo.FullName, false, MetroDialogOptions);
                        ProcessUITasks();
                    }

                    var destFile = fInfo.FullName + ".sp";
                    var standardOutput = new StringBuilder();
                    using var process = new Process();
                    process.StartInfo.WorkingDirectory = Paths.GetLysisDirectory();
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.FileName = "java";

                    process.StartInfo.Arguments = $"-jar lysis-java.jar \"{fInfo.FullName}\"";

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
                        await this.ShowMessageAsync($"{fInfo.Name} failed to decompile",
                            $"{ex.Message}", MessageDialogStyle.Affirmative,
                        MetroDialogOptions);
                    }

                    TryLoadSourceFile(destFile, true, false, true);
                    if (task != null)
                    {
                        await task.CloseAsync();
                    }
                }
            }
        }

        private void Command_OpenSPDef()
        {
            var spDefinitionWindow = new SPDefinitionWindow { Owner = this, ShowInTaskbar = false };
            spDefinitionWindow.ShowDialog();
        }
    }
}