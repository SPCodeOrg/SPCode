using System.Collections.Generic;
using System.Xml;
using SPCode.Utils;

namespace SPCode.Interop
{
    public class HotkeyControl
    {
        public static Dictionary<string, string> DefaultHotkeys = new()
        {
            { "New", "Ctrl+N" },
            { "NewTemplate", "Ctrl+Shift+N" },
            { "Open", "Ctrl+O" },
            { "Save", "Ctrl+S" },
            { "SaveAll", "Ctrl+Shift+S" },
            { "SaveAs", "Ctrl+Alt+S" },
            { "Close", "Ctrl+W" },
            { "CloseAll", "Ctrl+Shift+W" },
            { "FoldingsExpand", "Ctrl+P" },
            { "FoldingsCollapse", "Ctrl+Shift+P" },
            { "ReformatCurrent", "Ctrl+R" },
            { "ReformatAll", "Ctrl+Shift+R" },
            { "GoToLine", "Ctrl+G" },
            { "CommentLine", "Ctrl+K" },
            { "DeleteLine", "Ctrl+D" },
            { "MoveLineDown", "Ctrl+Down"},
            { "MoveLineUp", "Ctrl+Up"},
            { "DupeLineDown", "Ctrl+Alt+Down"},
            { "DupeLineUp", "Ctrl+Alt+Up"},
            { "SearchReplace", "Ctrl+F" },
            { "SearchDefinition", "Ctrl+Shift+F" },
            { "CompileCurrent", "F6" },
            { "CompileAll", "F5" },
            { "CopyPlugins", "F7" },
            { "UploadFTP", "F8" },
            { "StartServer", "F9" },
            { "SendRCON", "F10" },
        };

        public static Dictionary<string, string> RestrictedHotkeys = new()
        {
            { "Paste", "Ctrl+V" },
            { "Copy", "Ctrl+C" },
            { "Cut", "Ctrl+X" },
            { "Undo", "Ctrl+Z" },
            { "Redo", "Ctrl+Y" },
            { "SelectAll", "Ctrl+A" }
        };

        public static void BufferHotkeys()
        {
            // Buffer hotkeys in global HotkeyInfo list
            Program.HotkeysList = new List<HotkeyInfo>();
            var document = new XmlDocument();
            document.Load(Constants.HotkeysFile);

            foreach (XmlNode node in document.ChildNodes[0].ChildNodes)
            {
                Program.HotkeysList.Add(new HotkeyInfo(new Hotkey(node.InnerText), node.Name));
            }
        }

        public static void CreateDefaultHotkeys()
        {
            // Create the XML document
            var document = new XmlDocument();

            var rootElement = document.CreateElement(string.Empty, "Hotkeys", string.Empty);
            document.AppendChild(rootElement);

            foreach (var item in DefaultHotkeys)
            {
                var element = document.CreateElement(string.Empty, item.Key, string.Empty);
                var text = document.CreateTextNode(item.Value);
                element.AppendChild(text);
                rootElement.AppendChild(element);
            }

            // Buffer hotkeys in global HotkeyInfo list
            Program.HotkeysList = new List<HotkeyInfo>();
            foreach (XmlNode node in document.ChildNodes[0].ChildNodes)
            {
                Program.HotkeysList.Add(new HotkeyInfo(new Hotkey(node.InnerText), node.Name));
            }

            document.Save(Constants.HotkeysFile);
        }
    }
}
