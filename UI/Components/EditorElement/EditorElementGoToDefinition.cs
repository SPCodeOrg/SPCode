using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using SourcepawnCondenser.SourcemodDefinition;
using SPCode.Interop;

namespace SPCode.UI.Components
{
    public partial class EditorElement
    {

        private SMDefinition currentSmDef;

        public async Task GoToDefinition(MouseButtonEventArgs e)
        {
            try
            {
                var word = GetWordAtMousePosition(e);
                if (word.Trim().Length == 0)
                {
                    return;
                }

                e.Handled = true;

                // First search across all scripting directories

                var sm = MatchDefinition(Program.Configs[Program.SelectedConfig].GetSMDef(), word, e);
                if (sm != null)
                {
                    var config = Program.Configs[Program.SelectedConfig].SMDirectories;

                    foreach (var cfg in config)
                    {
                        var file = Path.GetFullPath(Path.Combine(cfg, "include", sm.File)) + ".inc";

                        if (!File.Exists(file))
                        {
                            file = Path.GetFullPath(Path.Combine(cfg, sm.File)) + ".inc";
                        }

                        await Task.Delay(100);
                        if (Program.MainWindow.TryLoadSourceFile(file, out var newEditor, true, false, true) && newEditor != null)
                        {
                            newEditor.editor.TextArea.Caret.Offset = sm.Index;
                            newEditor.editor.TextArea.Caret.BringCaretToView();
                            newEditor.editor.TextArea.Selection = Selection.Create(newEditor.editor.TextArea, sm.Index, sm.Index + sm.Length);
                            return;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                // If not, try to match variables in the current file 
                // (shit solution to fix some symbols getting read first inside of the file inaproppiately)

                sm = MatchDefinition(currentSmDef, word, e, true);
                if (sm != null)
                {
                    editor.TextArea.Caret.Offset = sm.Index;
                    editor.TextArea.Caret.BringCaretToView();
                    await Task.Delay(100);
                    editor.TextArea.Selection = Selection.Create(editor.TextArea, sm.Index, sm.Index + sm.Length);
                }
            }
            catch (Exception ex)
            {
                LoggingControl.LogAction($"Exception caught on go to definition: {ex.Message}. Report this bug!");
                return;
            }
        }

        private SMBaseDefinition MatchDefinition(SMDefinition smDef, string word, MouseButtonEventArgs e, bool currentFile = false)
        {
            if (smDef == null)
            {
                return null;
            }

            var mousePosition = editor.GetPositionFromPoint(e.GetPosition(this));

            if (mousePosition == null)
            {
                return null;
            }

            var line = mousePosition.Value.Line;
            var column = mousePosition.Value.Column;
            var offset = editor.TextArea.Document.GetOffset(line, column);

            // Begin attempting to match the supplied word with a definition

            // functions
            var sm = (SMBaseDefinition)smDef.Functions.FirstOrDefault(i => i.Name == word);

            // search in the same file if specified
            if (currentFile)
            {
                sm ??= smDef.Functions.FirstOrDefault(
                    func => func.Index <= offset &&
                            offset <= func.EndPos)
                    ?.FuncVariables?.FirstOrDefault(
                        i => i.Name.Equals(word));
            }

            // variables
            sm ??= smDef.Variables.FirstOrDefault(i => i.Name.Equals(word));

            // constants
            sm ??= smDef.Constants.FirstOrDefault(i => i.Name.Equals(word));

            // defines
            sm ??= smDef.Defines.FirstOrDefault(i => i.Name.Equals(word));

            // enums
            sm ??= smDef.Enums.FirstOrDefault(i => i.Name.Equals(word));

            if (sm == null)
            {
                foreach (var smEnum in smDef.Enums)
                {
                    var str = smEnum.Entries.FirstOrDefault(i => i.Equals(word));

                    if (str == null)
                    {
                        continue;
                    }

                    sm = smEnum;
                    break;
                }
            }

            // enum structs
            sm ??= smDef.EnumStructs.FirstOrDefault(i => i.Name.Equals(word, StringComparison.InvariantCultureIgnoreCase));

            sm ??= smDef.EnumStructs.FirstOrDefault(i => i.Fields.Any(j => j.Name == word));

            sm ??= smDef.EnumStructs.FirstOrDefault(i => i.Methods.Any(j => j.Name == word));

            // methodmaps
            sm ??= smDef.Methodmaps.FirstOrDefault(i => i.Name.Equals(word, StringComparison.InvariantCultureIgnoreCase));

            sm ??= smDef.Methodmaps.FirstOrDefault(i => i.Fields.Any(j => j.Name == word));

            sm ??= smDef.Methodmaps.FirstOrDefault(i => i.Methods.Any(j => j.Name == word));

            // structs?
            sm ??= smDef.Structs.FirstOrDefault(i => i.Name.Equals(word, StringComparison.InvariantCultureIgnoreCase));

            // typedefs
            sm ??= smDef.Typedefs.FirstOrDefault(i => i.Name.Equals(word, StringComparison.InvariantCultureIgnoreCase));

            return sm;
        }

        private string GetWordAtMousePosition(MouseEventArgs e)
        {
            var mousePosition = editor.GetPositionFromPoint(e.GetPosition(this));

            if (mousePosition == null)
            {
                return string.Empty;
            }

            var line = mousePosition.Value.Line;
            var column = mousePosition.Value.Column;
            var offset = editor.TextArea.Document.GetOffset(line, column);

            if (offset >= editor.TextArea.Document.TextLength)
            {
                offset--;
            }

            var offsetStart = TextUtilities.GetNextCaretPosition(editor.TextArea.Document, offset,
                LogicalDirection.Backward, CaretPositioningMode.WordBorder);
            var offsetEnd = TextUtilities.GetNextCaretPosition(editor.TextArea.Document, offset,
                LogicalDirection.Forward, CaretPositioningMode.WordBorder);

            if (offsetEnd == -1 || offsetStart == -1)
            {
                return string.Empty;
            }

            var currentChar = editor.TextArea.Document.GetText(offset, 1);

            if (string.IsNullOrWhiteSpace(currentChar))
            {
                return string.Empty;
            }

            return editor.TextArea.Document.GetText(offsetStart, offsetEnd - offsetStart);
        }
    }
}