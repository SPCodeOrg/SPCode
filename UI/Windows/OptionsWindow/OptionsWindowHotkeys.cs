﻿using SPCode.UI.Components;
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
using System.Windows.Controls;
using System.Diagnostics;

namespace SPCode.UI.Windows
{
    public partial class OptionsWindow
    {
        private readonly DispatcherTimer SaveHotkeyTimer;
        private HotkeyEditorControl _ctrl;
        private Hotkey _currentControlHotkey;
        private FontStyle _currentFontStyle;

        private void Hotkey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
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
            _currentFontStyle = _ctrl.FontStyle;

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

                // Check if the user has cleared the hotkey field
                if (_ctrl.Hotkey == null)
                {
                    Program.HotkeysList.FirstOrDefault(x => x.Command == _ctrl.Name.Substring(2)).Hotkey = null;
                    _ctrl.FontStyle = FontStyles.Italic;
                    break;
                }

                // Check if the received hotkey is not already assigned
                else if (_ctrl.Hotkey != null && hkInfo.Hotkey != null && hkInfo.Hotkey.ToString() == _ctrl.Hotkey.ToString() && hkInfo.Command != _ctrl.Name.Substring(2))
                {
                    _ctrl.Hotkey = _currentControlHotkey;
                    _ctrl.FontStyle = _currentFontStyle;
                    ShowLabel("In use!");
                    return;
                }

                // Check if the attempted hotkey is not restricted
                else if (HotkeyControl.RestrictedHotkeys.Where(x => _ctrl.Hotkey != null && x.Value.Equals(_ctrl.Hotkey.ToString())).Count() > 0)
                {
                    _ctrl.Hotkey = _currentControlHotkey;
                    _ctrl.FontStyle = _currentFontStyle;
                    ShowLabel("Reserved!");
                    return;
                }
                else
                {
                    _ctrl.FontStyle = FontStyles.Normal;
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
                    if (_ctrl.Hotkey == null)
                    {
                        entry.InnerText = "None";
                        document.Save(Constants.HotkeysFile);
                        break;
                    }
                    else
                    {
                        entry.InnerText = _ctrl.Hotkey.ToString();
                        document.Save(Constants.HotkeysFile);
                        break;
                    }
                }
            }

            // Buffer new hotkey
            foreach (var hkInfo in Program.HotkeysList)
            {
                if (hkInfo.Command == _ctrl.Name.Substring(2))
                {
                    hkInfo.Hotkey = _ctrl.Hotkey;
                    break;
                }
            }

            // Update InputGestureText of MenuItem
            foreach (var control in Program.MainWindow.MenuItems)
            {
                foreach (var item in control.Items)
                {
                    if (item is MenuItem && (item as MenuItem).Name == $"MenuI_{_ctrl.Name.Substring(2)}")
                    {
                        (item as MenuItem).InputGestureText = _ctrl.Hotkey == null ? "" : _ctrl.Hotkey.ToString();
                        return;
                    }
                }
            }
        }

        private void LoadHotkeysToSettings()
        {
            foreach (var hkInfo in Program.HotkeysList)
            {
                foreach (var control in HotkeysGrid.Children)
                {
                    if (control is HotkeyEditorControl)
                    {
                        var castCtrl = control as HotkeyEditorControl;
                        if (castCtrl.Name.Substring(2) == hkInfo.Command)
                        {
                            castCtrl.Hotkey = hkInfo.Hotkey;
                            if (castCtrl.Hotkey == null || castCtrl.Hotkey.ToString() == "None")
                            {
                                castCtrl.FontStyle = FontStyles.Italic;
                            }
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