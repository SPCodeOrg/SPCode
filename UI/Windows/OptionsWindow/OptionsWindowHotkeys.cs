using SPCode.UI.Components;
using SPCode.Interop;
using System.IO;
using System.Xml;
using System;
using SPCode.Utils;
using System.Windows.Threading;
using System.Windows.Input;

namespace SPCode.UI.Windows
{
    public partial class OptionsWindow
    {
        private readonly DispatcherTimer SaveHotkeyTimer;
        private HotkeyEditorControl _ctrl;

        private void Hotkey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            _ctrl = sender as HotkeyEditorControl;

            SaveHotkeyTimer.Stop();
            SaveHotkeyTimer.Start();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            SaveHotkeyTimer.Stop();
            SaveHotkey();
        }

        private void LoadHotkeys()
        {
            if (!File.Exists("Hotkeys.xml"))
            {
                HotkeyControl.CreateDefaultHotkeys();
            }

            var document = new XmlDocument();
            document.Load("Hotkeys.xml");

            // Loop through all the command entries in the XML
            foreach (XmlNode entry in document.ChildNodes[0].ChildNodes)
            {
                // Loop through all HotkeyControls
                foreach (var control in HotkeysGrid.Children)
                {
                    if (control is HotkeyEditorControl)
                    {
                        // Assign to every HotkeyControl a new Hotkey object
                        // whose contents are based on the command entry from the XML
                        var castedControl = control as HotkeyEditorControl;
                        if (castedControl.Name.Substring(2) == entry.Name)
                        {
                            castedControl.Hotkey = new Hotkey(entry.InnerText);
                        }
                    }
                }
            }
        }

        private void SaveHotkey()
        {
            if (!File.Exists("Hotkeys.xml"))
            {
                HotkeyControl.CreateDefaultHotkeys();
            }

            var document = new XmlDocument();
            document.Load("Hotkeys.xml");

            foreach (XmlNode entry in document.ChildNodes[0].ChildNodes)
            {
                if (entry.Name == _ctrl.Name.Substring(2))
                {
                    entry.InnerText = _ctrl.Hotkey.ToString();
                }
            }

            document.Save("Hotkeys.xml");
        }
    }
}
