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
            //if (!e.IsDown)
            //{
            //    return;
            //}

            var key = e.Key;

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

        private void SaveHotkey()
        {
            if (!File.Exists(Constants.HotkeysFile))
            {
                HotkeyControl.CreateDefaultHotkeys();
            }

            foreach (var hkInfo in Program.HotkeysList)
            {
                // Check if the received hotkey is not already assigned
                if (hkInfo.Hotkey.ToString() == _ctrl.Hotkey.ToString() && hkInfo.Command != _ctrl.Name.Substring(2))
                {
                    _ctrl.Hotkey = _currentControlHotkey;
                    ShowLabel("In use!");
                    return;
                }

                // Check if the attempted hotkey is not restricted
                else if (HotkeyControl.RestrictedHotkeys.Where(x => x.Value.Equals(_ctrl.Hotkey.ToString())).Count() > 0)
                {
                    _ctrl.Hotkey = _currentControlHotkey;
                    ShowLabel("Reserved!");
                    return;
                }
                else
                {
                    HideLabel();
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
                    break;
                }
            }

            document.Save(Constants.HotkeysFile);

            // Buffer new hotkey
            foreach (var hkInfo in Program.HotkeysList)
            {
                if (hkInfo.Command == _ctrl.Name.Substring(2))
                {
                    hkInfo.Hotkey = _ctrl.Hotkey;
                    break;
                }
            }

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

        private void ShowLabel(string message)
        {
            LblDisallowed.Visibility = Visibility.Visible;
            LblDisallowed.Margin = new Thickness(_ctrl.Margin.Left + 140.0, _ctrl.Margin.Top, _ctrl.Margin.Right, _ctrl.Margin.Bottom);
            LblDisallowed.Content = message;
            LblDisallowed.Foreground = new SolidColorBrush(Colors.Red);
            LblDisallowed.FontWeight = FontWeights.Bold;
        }

        private void HideLabel()
        {
            LblDisallowed.Visibility = Visibility.Collapsed;
        }
    }
}
