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

        private void MainWindowEvent_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

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
            var hotkeyInfo = Program.HotkeysList.FirstOrDefault(x => x.Hotkey.ToString() == hk.ToString());
            if (hotkeyInfo != null)
            {
                ExecuteCommand(hotkeyInfo.Command);
            }
        }

        private void ExecuteCommand(string command)
        {
            switch (command)
            {
                case "New": 
                    Command_New();
                    break;
                case "NewTemplate":
                    Command_NewFromTemplate();
                    break;
                case "Open":
                    Command_Open();
                    break;
                case "Save":
                    Command_Save();
                    break;
                case "SaveAll":
                    Command_SaveAll();
                    break;
                case "SaveAs":
                    Command_SaveAs();
                    break;
                case "Close":
                    Command_Close();
                    break;
                case "CloseAll":
                    Command_CloseAll();
                    break;
                case "FoldingsExpand":
                    Command_FlushFoldingState(true);
                    break;
                case "FoldingsCollapse":
                    Command_FlushFoldingState(false);
                    break;
                case "ReformatCurrent":
                    Command_TidyCode(false);
                    break;
                case "ReformatAll":
                    Command_TidyCode(true);
                    break;
                case "GoToLine":
                    Command_GoToLine();
                    break;
                case "CommentLine":
                    Command_ToggleCommentLine();
                    break;
                case "SearchReplace":
                    Command_FindReplace();
                    break;
                case "SearchDefinition":
                    Command_OpenSPDef();
                    break;
                case "CompileCurrent":
                    Compile_SPScripts(false);
                    break;
                case "CompileAll":
                    Compile_SPScripts();
                    break;
                case "CopyPlugins":
                    Copy_Plugins();
                    break;
                case "UploadFTP":
                    FTPUpload_Plugins();
                    break;
                case "StartServer":
                    Server_Start();
                    break;
                case "SendRCON":
                    Server_Query();
                    break;
            }
        }
    }
}