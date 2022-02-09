using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using SPCode.UI.Components;
using SPCode.UI.Windows;
using SPCode.Utils;
using SPCode.Utils.SPSyntaxTidy;
using static SPCode.Interop.TranslationProvider;

namespace SPCode.UI
{
    public partial class MainWindow
    {
        /// <summary>
        /// Gets the current editor element.
        /// </summary>
        /// <returns></returns>
        public EditorElement GetCurrentEditorElement()
        {
            if (Application.Current != null)
            {
                foreach (Window win in Application.Current.Windows)
                {
                    if (win is NewFileWindow newFileWin)
                    {
                        return newFileWin.PreviewBox;
                    }
                }
            }

            if (DockingPane.SelectedContent?.Content != null)
            {
                var possElement = DockingManager.ActiveContent;
                if (possElement is EditorElement element)
                {
                    return element;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the current DASM element.
        /// </summary>
        /// <returns></returns>
        public DASMElement GetCurrentDASMElement()
        {
            DASMElement outElement = null;
            if (DockingPane.SelectedContent?.Content != null)
            {
                var possElement = DockingManager.ActiveContent;
                if (possElement is DASMElement element)
                {
                    outElement = element;
                }
            }

            return outElement;
        }

        /// <summary>
        /// Gets an array of all open editor elements.
        /// </summary>
        /// <returns></returns>
        public List<EditorElement> GetAllEditorElements()
        {
            return EditorReferences.Count == 0 ? null : EditorReferences;
        }

        /// <summary>
        /// Gets an array of all open DASM elements.
        /// </summary>
        /// <returns></returns>
        public List<DASMElement> GetAllDASMElements()
        {
            return DASMReferences.Count < 1 ? null : DASMReferences;
        }

        /// <summary>
        /// Creates a new SourcePawn Script file and loads it.
        /// </summary>
        private void Command_New()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                if (ee != null && ee.IsTemplateEditor)
                {
                    return;
                }
                string newFilePath;
                var newFileNum = 0;
                do
                {
                    newFilePath = Path.Combine(Program.Configs[Program.SelectedConfig].SMDirectories[0], $"New Plugin ({++newFileNum}).sp");
                } while (File.Exists(newFilePath));

                File.Create(newFilePath).Close();

                AddEditorElement(new FileInfo(newFilePath), $"New Plugin ({newFileNum}).sp", true, out _);
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }

        }

        /// <summary>
        /// Opens a new window for the user to create a new SourcePawn Script from a template.
        /// </summary>
        private void Command_NewFromTemplate()
        {
            try
            {
                var nfWindow = new NewFileWindow
                {
                    Owner = this,
                    ShowInTaskbar = false
                };
                nfWindow.ShowDialog();
                UpdateWindowTitle();
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }

        }

        /// <summary>
        /// Opens the Open File window for the user to load a file into the editor.
        /// </summary>
        private void Command_Open()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                if (ee != null && ee.IsTemplateEditor)
                {
                    return;
                }
                var ofd = new OpenFileDialog
                {
                    AddExtension = true,
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Filter = Constants.FileOpenFilters,
                    Multiselect = true,
                    Title = Translate("OpenNewFile")
                };
                var result = ofd.ShowDialog(this);
                if (result.Value)
                {
                    var AnyFileLoaded = false;
                    if (ofd.FileNames.Length > 0)
                    {
                        for (var i = 0; i < ofd.FileNames.Length; ++i)
                        {
                            AnyFileLoaded |= TryLoadSourceFile(ofd.FileNames[i], out _, i == 0, true, i == 0);
                        }

                        if (!AnyFileLoaded)
                        {
                            MetroDialogOptions.ColorScheme = MetroDialogColorScheme.Theme;
                            this.ShowMessageAsync(Translate("NoFileOpened"),
                                Translate("NoFileOpenedCap"), MessageDialogStyle.Affirmative,
                                MetroDialogOptions);
                        }
                    }
                }

                Activate();
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Saves the current file.
        /// </summary>
        private void Command_Save()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                if (ee != null && !ee.IsTemplateEditor)
                {
                    ee.Save(true);
                    BlendOverEffect.Begin();
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Opens the Save As window.
        /// </summary>
        private void Command_SaveAs()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                if (ee != null && !ee.IsTemplateEditor)
                {
                    var sfd = new SaveFileDialog { AddExtension = true, Filter = Constants.FileSaveFilters, OverwritePrompt = true, Title = Translate("SaveFileAs"), FileName = ee.Parent.Title.Trim('*') };
                    var result = sfd.ShowDialog(this);
                    if (result.Value && !string.IsNullOrWhiteSpace(sfd.FileName))
                    {
                        ee.FullFilePath = sfd.FileName;
                        ee.Save(true);
                        BlendOverEffect.Begin();
                    }
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Opens the Go To Line window.
        /// </summary>
        private void Command_GoToLine()
        {
            try
            {
                if (GetAllEditorElements() != null)
                {
                    var goToLineWindow = new GoToLineWindow();
                    goToLineWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Deletes the line the caret is in.
        /// </summary>
        private void Command_DeleteLine()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                if (ee != null && !ee.editor.IsReadOnly)
                {
                    ee.DeleteLine();
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Opens the Search window.
        /// </summary>
        private void Command_FindReplace()
        {
            try
            {
                if (GetAllEditorElements() == null)
                {
                    return;
                }

                var selection = GetCurrentEditorElement().editor.TextArea.Selection.GetText();

                foreach (Window win in Application.Current.Windows)
                {
                    if (win is FindReplaceWindow findWin)
                    {
                        findWin.Activate();
                        findWin.FindBox.Text = selection;
                        findWin.FindBox.SelectAll();
                        findWin.FindBox.Focus();
                        return;
                    }
                }
                var findWindow = new FindReplaceWindow(selection) { Owner = this };
                findWindow.Show();
                findWindow.FindBox.Focus();
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Saves all opened files.
        /// </summary>
        private void Command_SaveAll()
        {
            try
            {
                var editors = GetAllEditorElements();
                if (editors == null || GetCurrentEditorElement().IsTemplateEditor)
                {
                    return;
                }

                if (editors.Count > 0)
                {
                    foreach (var editor in editors)
                    {
                        editor.Save();
                    }

                    BlendOverEffect.Begin();
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Closes the current file opened.
        /// </summary>
        private void Command_Close()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                var de = GetCurrentDASMElement();
                if (ee != null && (ee.IsTemplateEditor || ee.ClosingPromptOpened))
                {
                    return;
                }
                ee?.Close();
                de?.Close();
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Closes all files opened.
        /// </summary>
        private async void Command_CloseAll()
        {
            try
            {
                var editors = GetAllEditorElements();
                var dasm = GetAllDASMElements();

                if (editors == null || editors.Any(x => x.IsTemplateEditor) || editors.Count == 0 || editors.Any(x => x.ClosingPromptOpened))
                {
                    return;
                }

                editors.ToList().ForEach(x => x.Close());
                dasm?.ToList().ForEach(y => y.Close());
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Undoes the previous action.
        /// </summary>
        private void Command_Undo()
        {
            try
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
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Redoes the latter action.
        /// </summary>
        private void Command_Redo()
        {
            try
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
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Cut command.
        /// </summary>
        private void Command_Cut()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                ee?.editor.Cut();
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Copy command.
        /// </summary>
        private void Command_Copy()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                ee?.editor.Copy();
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Paste command.
        /// </summary>
        private void Command_Paste()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                ee?.editor.Paste();
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Collapses or expands all foldings in the editor.
        /// </summary>
        /// <param name="folded">Whether to fold all foldings (true to collapse all).</param>
        private void Command_FlushFoldingState(bool folded)
        {
            try
            {
                var ee = GetCurrentEditorElement();
                if (ee?.foldingManager != null)
                {
                    var foldings = ee.foldingManager.AllFoldings;
                    foreach (var folding in foldings)
                    {
                        folding.IsFolded = folded;
                    }
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Select all command.
        /// </summary>
        private void Command_SelectAll()
        {
            try
            {
                var ee = GetCurrentEditorElement();
                ee?.editor.SelectAll();
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Comment or uncomment the line the caret is in.
        /// </summary>
        private void Command_ToggleCommentLine(bool comment)
        {
            try
            {
                var ee = GetCurrentEditorElement();
                if (ee != null && !ee.editor.IsReadOnly)
                {
                    ee.ToggleComment(comment);
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Change the case of the current selection.
        /// </summary>
        /// <param name="toUpper">Whether to transform to uppercase</param>
        public void Command_ChangeCase(bool toUpper)
        {
            try
            {
                var ee = GetCurrentEditorElement();
                if (ee != null && !ee.editor.IsReadOnly)
                {
                    ee.ChangeCase(toUpper);
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Perform a code reformat to clean loose whitespaces/wrongly indented code.
        /// </summary>
        /// <param name="All"></param>
        private void Command_TidyCode(bool All)
        {
            try
            {
                var editors = All ? GetAllEditorElements() : new() { GetCurrentEditorElement() };
                if (editors == null)
                {
                    return;
                }
                foreach (var ee in editors)
                {
                    if (ee != null && !ee.editor.IsReadOnly)
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
                        }
                        ee.editor.TextArea.Caret.Offset = newCaretPos;
                    }
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Opens the Open File window for the user to select a file to decompile.
        /// </summary>
        private async void Command_Decompile()
        {
            try
            {
                var file = DecompileUtil.GetFile();
                var msg = await this.ShowProgressAsync(Translate("Decompiling") + "...", file.Name, false, MetroDialogOptions);
                msg.SetIndeterminate();
                ProcessUITasks();
                TryLoadSourceFile(DecompileUtil.GetDecompiledPlugin(file), out _);
                await msg.CloseAsync();
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }

        /// <summary>
        /// Opens the Search Definition window.
        /// </summary>
        private void Command_OpenSPDef()
        {
            try
            {
                var spDefinitionWindow = new SPDefinitionWindow
                {
                    Owner = this,
                    ShowInTaskbar = false
                };
                spDefinitionWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }
        /// <summary>
        /// Re-opens the last closed tab
        /// </summary>
        private void Command_ReopenLastClosedTab()
        {
            try
            {
                if (Program.RecentFilesStack.Count > 0)
                {
                    TryLoadSourceFile(Program.RecentFilesStack.Pop(), out _, true, false, true);
                }

                MenuI_ReopenLastClosedTab.IsEnabled = Program.RecentFilesStack.Count > 0;
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(Translate("Error"), ex.Message, settings: MetroDialogOptions);
            }
        }
    }
}