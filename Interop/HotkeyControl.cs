using System.Collections.Generic;
using System.Xml;
using SPCode.Utils;

namespace SPCode.Interop
{
    public class HotkeyControl
    {
        public static Dictionary<string, string> DefaultHotkeys = new()
        {
            { "New", "Control+N" },
            { "NewTemplate", "Control+Shift+N" },
            { "Open", "Control+O" },
            { "Save", "Control+S" },
            { "SaveAll", "Control+Shift+S" },
            { "SaveAs", "Control+Alt+S" },
            { "Close", "Control+W" },
            { "CloseAll", "Control+Shift+W" },
            { "FoldingsExpand", "Control+P" },
            { "FoldingsCollapse", "Control+Shift+P" },
            { "ReformatCurrent", "Control+R" },
            { "ReformatAll", "Control+Shift+R" },
            { "GoToLine", "Control+G" },
            { "CommentLine", "Control+K" },
            { "SearchReplace", "Control+F" },
            { "SearchDefinition", "Control+Shift+F" },
            { "CompileCurrent", "F6" },
            { "CompileAll", "F5" },
            { "CopyPlugins", "F7" },
            { "UploadFTP", "F8" },
            { "StartServer", "F9" },
            { "SendRCON", "F10" },
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
