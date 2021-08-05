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
        //some key bindings are handled in EditorElement.xaml.cs because the editor will fetch some keys before they can be handled here.

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
                { "SearchReplace", Command_FindReplace },
                { "SearchDefinition", Command_OpenSPDef },
                { "CompileCurrent", () => Compile_SPScripts(false) },
                { "CompileAll", () => Compile_SPScripts() },
                { "CopyPlugins", () => Copy_Plugins() },
                { "UploadFTP", FTPUpload_Plugins },
                { "StartServer", Server_Start },
                { "SendRCON", Server_Query }
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

            if (key == Key.LeftCtrl ||
                key == Key.RightCtrl ||
                key == Key.LeftAlt ||
                key == Key.RightAlt ||
                key == Key.LeftShift ||
                key == Key.RightShift ||
                key == Key.LWin ||
                key == Key.RWin ||
                key == Key.Clear ||
                key == Key.OemClear ||
                key == Key.Apps)
            {
                return;
            }

            ProcessHotkey(new Hotkey(key, modifiers));
        }

        private void ProcessHotkey(Hotkey hk)
        {
            var hotkeyInfo = Program.HotkeysList.FirstOrDefault(x => x.Hotkey != null && x.Hotkey.ToString() == hk.ToString());
            if (hotkeyInfo != null)
            {
                Commands[hotkeyInfo.Command]();
            }
        }
    }
}