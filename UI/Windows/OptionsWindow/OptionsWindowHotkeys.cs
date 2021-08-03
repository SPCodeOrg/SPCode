using SPCode.UI.Components;
using SPCode.Interop;
using System.IO;
using System.Xml;
using System;
using SPCode.Utils;
using System.Windows.Threading;
using System.Windows.Input;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace SPCode.UI.Windows
{
    public partial class OptionsWindow
    {
        private readonly DispatcherTimer SaveHotkeyTimer;
        private HotkeyEditorControl _ctrl;
        private Hotkey _currentControlHotkey;

        private void Hotkey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            _ctrl = sender as HotkeyEditorControl;
            _currentControlHotkey = _ctrl.Hotkey;

            SaveHotkeyTimer.Stop();
            SaveHotkeyTimer.Start();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            SaveHotkeyTimer.Stop();
            SaveHotkey();
        }

        private void LoadHotkeysToSettings()
        {
            if (!File.Exists(Constants.HotkeysFile))
            {
                HotkeyControl.CreateDefaultHotkeys();
            }

            var document = new XmlDocument();
            document.Load(Constants.HotkeysFile);

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
            if (!File.Exists(Constants.HotkeysFile))
            {
                HotkeyControl.CreateDefaultHotkeys();
            }


            // Check if the received hotkey is not already assigned
            foreach (var hkInfo in Program.HotkeysList)
            {
                if (hkInfo.Hotkey.ToString() == _ctrl.Hotkey.ToString() && hkInfo.Command != _ctrl.Name.Substring(2))
                {
                    _ctrl.Hotkey = _currentControlHotkey;

                    LblDisallowed.Visibility = Visibility.Visible;
                    LblDisallowed.Margin = new Thickness(_ctrl.Margin.Left + 140.0, _ctrl.Margin.Top, _ctrl.Margin.Right, _ctrl.Margin.Bottom);
                    LblDisallowed.Content = "In use!";
                    LblDisallowed.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    LblDisallowed.FontWeight = FontWeights.Bold;
                    return;
                }
                else
                {
                    LblDisallowed.Visibility = Visibility.Collapsed;
                }
            }

            // Modify the XML document to update hotkey
            var document = new XmlDocument();
            document.Load(Constants.HotkeysFile);

            foreach (XmlNode entry in document.ChildNodes[0].ChildNodes)
            {
                if (entry.Name == _ctrl.Name.Substring(2))
                {
                    entry.InnerText = _ctrl.Hotkey.ToString();
                }
            }

            document.Save(Constants.HotkeysFile);

            // Buffer new hotkey in global HotkeyInfo list
            foreach (var hkInfo in Program.HotkeysList)
            {
                if (hkInfo.Command == _ctrl.Name.Substring(2))
                {
                    hkInfo.Hotkey = _ctrl.Hotkey;
                    return;
                }
            }

        }
    }
}
