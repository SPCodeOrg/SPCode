using System.Collections.Generic;
using System.Xml;

namespace SPCode.Interop
{
    public class HotkeyControl
    {
        public static Dictionary<string, string> DefaultCommands = new()
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
            { "CommentLine", "Control+/" },
            { "SearchReplace", "Control+F" },
            { "SearchDefinition", "Control+Shift+F" },
            { "CompileCurrent", "F6" },
            { "CompileAll", "F5" },
            { "CopyPlugins", "F7" },
            { "UploadFTP", "F8" },
            { "StartServer", "F9" },
            { "SendRCON", "F10" },
        };

        public static void CreateDefaultHotkeys()
        {
            var document = new XmlDocument();

            var rootElement = document.CreateElement(string.Empty, "Hotkeys", string.Empty);
            document.AppendChild(rootElement);

            foreach (var item in DefaultCommands)
            {
                var element = document.CreateElement(string.Empty, item.Key, string.Empty);
                var text = document.CreateTextNode(item.Value);
                element.AppendChild(text);
                rootElement.AppendChild(element);
            }

            document.Save("Hotkeys.xml");
        }
    }
}
