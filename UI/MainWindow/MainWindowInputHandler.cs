using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using SPCode.Utils;

namespace SPCode.UI
{
    public partial class MainWindow
    {
        public Dictionary<string, Action> Commands;

        private void LoadCommandsDictionary()
        {
            Commands = new()
            {
                { "New", Command_New },
                { "NewTemplate", Command_NewFromTemplate },
                { "Open", Command_Open },
                { "Save", Command_Save },
                { "SaveAll", Command_SaveAll },
                { "SaveAs", Command_SaveAs },
                { "Close", Command_Close },
                { "CloseAll", Command_CloseAll },
                { "FoldingsExpand", () => Command_FlushFoldingState(true) },
                { "FoldingsCollapse", () => Command_FlushFoldingState(false) },
                { "ReformatCurrent", () => Command_TidyCode(false) },
                { "ReformatAll", () => Command_TidyCode(true) },
                { "GoToLine", Command_GoToLine },
                { "CommentLine", Command_ToggleCommentLine },
                { "TransformUppercase", () => Command_ChangeCase(true) },
                { "TransformLowercase", () => Command_ChangeCase(false) },
                { "DeleteLine", Command_DeleteLine },
                { "MoveLineDown", () => GetCurrentEditorElement().MoveLine(true) },
                { "MoveLineUp", () => GetCurrentEditorElement().MoveLine(false) },
                { "DupeLineDown", () => GetCurrentEditorElement().DuplicateLine(true) },
                { "DupeLineUp", () => GetCurrentEditorElement().DuplicateLine(false) },
                { "SearchReplace", Command_FindReplace },
                { "SearchDefinition", Command_OpenSPDef },
                { "CompileCurrent", () => Compile_SPScripts(false) },
                { "CompileAll", () => Compile_SPScripts() },
                { "CopyPlugins", () => Copy_Plugins() },
                { "UploadFTP", FTPUpload_Plugins },
                { "StartServer", Server_Start },
                { "SendRCON", Server_Query },
            };
        }

        private void MainWindowEvent_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.IsDown)
            {
                return;
            }

            var key = e.Key;
            var modifiers = Keyboard.Modifiers;

            if (key == Key.System)
            {
                key = e.SystemKey;
            }

            if (!HotkeyUtils.IsKeyModifier(key))
            {
                ProcessHotkey(new Hotkey(key, modifiers));
            }

        }

        public void ProcessHotkey(Hotkey hk, KeyEventArgs e = null)
        {
            var hotkeyInfo = Program.HotkeysList.FirstOrDefault(x => x.Hotkey != null && x.Hotkey.ToString() == hk.ToString());
            if (hotkeyInfo != null)
            {
                Commands[hotkeyInfo.Command]();
                if (e != null)
                {
                    e.Handled = true;
                }
            }
        }
    }
}