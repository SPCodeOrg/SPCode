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
        public EditorElement[] GetAllEditorElements()
        {
            return EditorsReferences.Count < 1 ? null : EditorsReferences.ToArray();
        }

        /// <summary>
        /// Gets an array of all open DASM elements.
        /// </summary>
        /// <returns></returns>
        public DASMElement[] GetAllDASMElements()
        {
            return DASMReferences.Count < 1 ? null : DASMReferences.ToArray();
        }

        /// <summary>
        /// Creates a new SourcePawn Script file and loads it.
        /// </summary>
        private void Command_New()
        {
            string newFilePath;
            var newFileNum = 0;
            do
            {
                newFilePath = Path.Combine(Program.Configs[Program.SelectedConfig].SMDirectories[0], $"New Plugin ({++newFileNum}).sp");
            } while (File.Exists(newFilePath));

            File.Create(newFilePath).Close();

            AddEditorElement(newFilePath, $"New Plugin ({newFileNum}).sp", true, out _);
            RefreshObjectBrowser();
        }

        /// <summary>
        /// Opens a new window for the user to create a new SourcePawn Script from a template.
        /// </summary>
        private void Command_NewFromTemplate()
        {
            var nfWindow = new NewFileWindow
            {
                Owner = this,
                ShowInTaskbar = false
            };
            nfWindow.ShowDialog();
            UpdateWindowTitle();
        }

        /// <summary>
        /// Opens the Open File window for the user to load a file into the editor.
        /// </summary>
        private void Command_Open()
        {
            var ofd = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = Constants.FileOpenFilters,
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
                        AnyFileLoaded |= TryLoadSourceFile(ofd.FileNames[i], out _, i == 0, true, i == 0);
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

        /// <summary>
        /// Saves the current file.
        /// </summary>
        private void Command_Save()
        {
            var ee = GetCurrentEditorElement();
            if (ee != null)
            {
                ee.Save(true);
                BlendOverEffect.Begin();
            }
        }

        /// <summary>
        /// Opens the Save As window.
        /// </summary>
        private void Command_SaveAs()
        {
            var ee = GetCurrentEditorElement();
            if (ee != null)
            {
                var sfd = new SaveFileDialog
                {
                    AddExtension = true,
                    Filter = Constants.FileSaveFilters,
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

        /// <summary>
        /// Opens the Go To Line window.
        /// </summary>
        private void Command_GoToLine()
        {
            if (GetAllEditorElements() == null)
            {
                return;
            }
            var goToLineWindow = new GoToLineWindow();
            goToLineWindow.ShowDialog();
        }

        /// <summary>
        /// Deletes the line the caret is in.
        /// </summary>
        private void Command_DeleteLine()
        {
            var ee = GetCurrentEditorElement();
            if (ee != null)
            {
                ee.DeleteLine();
            }
        }

        /// <summary>
        /// Opens the Search window.
        /// </summary>
        private void Command_FindReplace()
        {
            if (GetAllEditorElements() == null)
            {
                return;
            }
            if (Program.IsSearchOpen)
            {
                foreach (Window win in Application.Current.Windows)
                {
                    if (win is FindReplaceWindow findWin)
                    {
                        findWin.Activate();
                        findWin.FindBox.Focus();
                        return;
                    }
                }
            }
            var findWindow = new FindReplaceWindow();
            Program.IsSearchOpen = true;
            findWindow.Show();
            findWindow.FindBox.Focus();
        }

        /// <summary>
        /// Saves all opened files.
        /// </summary>
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

        /// <summary>
        /// Closes the current file opened.
        /// </summary>
        private void Command_Close()
        {
            var ee = GetCurrentEditorElement();
            if (ee == null)
            {
                return;
            }

            DockingPane.RemoveChild(ee.Parent);
            ee.Close();
            UpdateOBFileButton();
        }

        /// <summary>
        /// Closes all files opened.
        /// </summary>
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

                    var result = await this.ShowMessageAsync(Program.Translations.GetLanguage("SaveFollow"),
                        str.ToString(), MessageDialogStyle.AffirmativeAndNegative, MetroDialogOptions);
                    if (result == MessageDialogResult.Affirmative)
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
            UpdateOBFileButton();
        }

        /// <summary>
        /// Undoes the previous action.
        /// </summary>
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

        /// <summary>
        /// Redoes the latter action.
        /// </summary>
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

        /// <summary>
        /// Cut command.
        /// </summary>
        private void Command_Cut()
        {
            var ee = GetCurrentEditorElement();
            ee?.editor.Cut();
        }

        /// <summary>
        /// Copy command.
        /// </summary>
        private void Command_Copy()
        {
            var ee = GetCurrentEditorElement();
            ee?.editor.Copy();
        }

        /// <summary>
        /// Paste command.
        /// </summary>
        private void Command_Paste()
        {
            var ee = GetCurrentEditorElement();
            ee?.editor.Paste();
        }

        /// <summary>
        /// Collapses or expands all foldings in the editor.
        /// </summary>
        /// <param name="folded">Whether to fold all foldings (true to collapse all).</param>
        private void Command_FlushFoldingState(bool folded)
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

        /// <summary>
        /// Select all command.
        /// </summary>
        private void Command_SelectAll()
        {
            var ee = GetCurrentEditorElement();
            ee?.editor.SelectAll();
        }

        /// <summary>
        /// Comment or uncomment the line the caret is in.
        /// </summary>
        private void Command_ToggleCommentLine()
        {
            GetCurrentEditorElement()?.ToggleCommentOnLine();
        }

        /// <summary>
        /// Change the case of the current selection.
        /// </summary>
        /// <param name="toUpper">Whether to transform to uppercase</param>
        public void Command_ChangeCase(bool toUpper)
        {
            GetCurrentEditorElement()?.ChangeCase(toUpper);
        }

        /// <summary>
        /// Perform a code reformat to clean loose whitespaces/wrongly indented code.
        /// </summary>
        /// <param name="All"></param>
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

        /// <summary>
        /// Opens the Open File window for the user to select a file to decompile.
        /// </summary>
        private async void Command_Decompile()
        {
            var decomp = new DecompileUtil();
            await decomp.DecompilePlugin();
        }

        /// <summary>
        /// Opens the Search Definition window.
        /// </summary>
        private void Command_OpenSPDef()
        {
            var spDefinitionWindow = new SPDefinitionWindow
            {
                Owner = this,
                ShowInTaskbar = false
            };
            spDefinitionWindow.ShowDialog();
        }
    }
}