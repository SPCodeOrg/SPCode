using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SPCode.Utils;

namespace SPCode.Interop;

public class HotkeyControl
{
    public static Dictionary<string, string> DefaultHotkeys = new()
    {
        { "New", "Ctrl+N" },
        { "NewTemplate", "Ctrl+Shift+N" },
        { "Open", "Ctrl+O" },
        { "ReopenLastClosedTab", "Ctrl+Shift+T" },
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
        { "UncommentLine", "Ctrl+Shift+K" },
        { "TransformUppercase", "Ctrl+U" },
        { "TransformLowercase", "Ctrl+Shift+U" },
        { "DeleteLine", "Ctrl+D" },
        { "MoveLineDown", "Ctrl+Down" },
        { "MoveLineUp", "Ctrl+Up" },
        { "DupeLineDown", "Ctrl+Alt+Down" },
        { "DupeLineUp", "Ctrl+Alt+Up" },
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

    /// <summary>
    /// Checks if there are new Hotkeys that haven't been added to the Hotkeys file <br>
    /// and buffers all Hotkeys to an array in memory.
    /// </summary>
    public static void CheckAndBufferHotkeys()
    {
        try
        {
            // Load the current Hotkeys file
            Program.HotkeysList = new();
            var document = new XmlDocument();
            document.Load(Constants.HotkeysFile);

            // Compare with the default hotkeys to check for new commands
            var xmlNodes = document.ChildNodes[0].ChildNodes.Cast<XmlNode>().ToList();
            var defaultHksCount = DefaultHotkeys.Count;

            // If the count is not equal, there's a new hotkey to be added to the file
            if (xmlNodes.Count != defaultHksCount)
            {
                // Regular for to get the index
                for (var i = 0; i < defaultHksCount; i++)
                {
                    var currentHk = DefaultHotkeys.ElementAt(i);
                    if (!xmlNodes.Any(x => x.Name.Equals(DefaultHotkeys.ElementAt(i).Key)))
                    {
                        var element = document.CreateElement(string.Empty, currentHk.Key, string.Empty);
                        var text = document.CreateTextNode(currentHk.Value);
                        element.AppendChild(text);
                        document.ChildNodes[0].InsertBefore(element, document.ChildNodes[0].ChildNodes[i]);
                    }
                }
                document.Save(Constants.HotkeysFile);
                xmlNodes = document.ChildNodes[0].ChildNodes.Cast<XmlNode>().ToList();
            }

            xmlNodes.ForEach(x =>
            {
                var hki = new HotkeyInfo(new Hotkey(x.InnerText), x.Name);
                Program.HotkeysList.Add(hki);
            });

        }
        catch (XmlException ex)
        {
            var invalidHotkeysFile = Constants.HotkeysFile + ".invalid";
            if (File.Exists(invalidHotkeysFile))
            {
                File.Delete(invalidHotkeysFile);
            }
            File.Move(Constants.HotkeysFile, invalidHotkeysFile);
            CreateDefaultHotkeys();
            MessageBox.Show("There was an error parsing the Hotkeys.xml file.\n" +
                $"It has been renamed to {invalidHotkeysFile}, and a new one was created.\n" +
                $"Details: {ex.Message}", "SPCode Error");
        }
        catch (Exception ex)
        {
            throw new Exception("Error while checking and buffering the hotkeys", ex);
        }
    }

    /// <summary>
    /// Creates the default hotkeys, stores them in an XML and buffers them.
    /// </summary>
    public static void CreateDefaultHotkeys()
    {
        try
        {
            // Create the XML document
            var document = new XmlDocument();

            var rootElement = document.CreateElement(string.Empty, "Hotkeys", string.Empty);
            document.AppendChild(rootElement);

            // Fill it with the default hotkeys from the dictionary
            foreach (var item in DefaultHotkeys)
            {
                var element = document.CreateElement(string.Empty, item.Key, string.Empty);
                var text = document.CreateTextNode(item.Value);
                element.AppendChild(text);
                rootElement.AppendChild(element);
            }

            // Buffer hotkeys in global HotkeyInfo list
            Program.HotkeysList = new();
            foreach (XmlNode node in document.ChildNodes[0].ChildNodes)
            {
                Program.HotkeysList.Add(new HotkeyInfo(new Hotkey(node.InnerText), node.Name));
            }

            document.Save(Constants.HotkeysFile);
        }
        catch (Exception ex)
        {
            throw new Exception("Error while creating the default hotkeys", ex);
        }
    }
}