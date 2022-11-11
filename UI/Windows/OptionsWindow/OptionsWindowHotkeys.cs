using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using SPCode.Interop;
using SPCode.UI.Components;
using SPCode.Utils;
using static SPCode.Interop.TranslationProvider;

namespace SPCode.UI.Windows;

public partial class OptionsWindow
{
    #region Variables
    private readonly DispatcherTimer SaveHotkeyTimer;
    private HotkeyEditorControl _ctrl;
    private Hotkey _currentControlHotkey;
    private FontStyle _currentFontStyle;
    #endregion

    #region Events
    private void Hotkey_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key;

        if (key == Key.System)
        {
            key = e.SystemKey;
        }

        if (!HotkeyUtils.IsKeyModifier(key))
        {
            _ctrl = sender as HotkeyEditorControl;
            _currentControlHotkey = _ctrl.Hotkey;
            _currentFontStyle = _ctrl.FontStyle;

            SaveHotkeyTimer.Stop();
            SaveHotkeyTimer.Start();
        }
    }

    private void Hotkey_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.RightButton != MouseButtonState.Pressed || sender is not HotkeyEditorControl hkControl)
        {
            return;
        }

        _ctrl = hkControl;
        _currentControlHotkey = _ctrl.Hotkey;

        var mi = new MenuItem
        {
            Header = Translate("DefaultValues")
        };
        var cm = new ContextMenu();
        cm.Items.Add(mi);
        hkControl.HotkeyTextBox.ContextMenu = cm;

        mi.Click += (sender, e) =>
        {
            foreach (var hk in Program.HotkeysList)
            {
                var hkStr = hk.Hotkey.ToString();
                if (hkStr == HotkeyControl.DefaultHotkeys[hkControl.Name.Substring(2)] && hkStr != hkControl.Hotkey.ToString())
                {
                    ShowLabel(Translate("DefaultTaken"));
                    return;
                }
            }

            HideLabel();
            SaveHotkey(true);
        };

    }

    private void OnTimerTick(object sender, EventArgs e)
    {
        SaveHotkeyTimer.Stop();
        SaveHotkey();
    }
    #endregion

    #region Methods
    /// <summary>
    /// Loads the hotkeys into the Options Window UI.
    /// </summary>
    private void LoadHotkeysSection()
    {
        var txtMargin = 54;
        var hkMargin = 48;
        var sepMargin = 61;
        foreach (var hkInfo in Program.HotkeysList)
        {
            var textBlock = new TextBlock()
            {
                Name = $"Txt{hkInfo.Command}",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(161, txtMargin, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Text = Translate(hkInfo.Command)
            };
            var hotkeyEditor = new HotkeyEditorControl()
            {
                Name = $"Hk{hkInfo.Command}",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Height = 28,
                Width = 140,
                Margin = new Thickness(85, hkMargin, 0, 0),
                Hotkey = hkInfo.Hotkey,
                FontStyle = hkInfo.Hotkey == null || hkInfo.Hotkey.ToString() == "None" ? FontStyles.Italic : FontStyles.Normal
            };
            hotkeyEditor.PreviewKeyDown += Hotkey_PreviewKeyDown;
            hotkeyEditor.PreviewMouseDown += Hotkey_PreviewMouseDown;
            Grid.SetColumn(hotkeyEditor, 1);
            var separator = new Separator()
            {
                Margin = new Thickness(30, sepMargin, 0, 0),
                Height = 41,
                VerticalAlignment = VerticalAlignment.Top,
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#408B8B8B")
            };
            Grid.SetColumnSpan(separator, 2);

            HotkeysGrid.Children.Add(textBlock);
            HotkeysGrid.Children.Add(hotkeyEditor);
            HotkeysGrid.Children.Add(separator);

            txtMargin += 40;
            hkMargin += 40;
            sepMargin += 40;
        }
    }

    /// <summary>
    /// Saves the input hotkey to the Hotkeys file, and caches it.
    /// </summary>
    private void SaveHotkey(bool toDefault = false)
    {
        // Create the hotkeys file if it doesn't exist for some reason
        if (!File.Exists(Constants.HotkeysFile))
        {
            HotkeyControl.CreateDefaultHotkeys();
        }

        // Get the control name by separating it from its x:Name suffix
        var ctrlName = _ctrl.Name.Substring(2);

        // We skip checks if we're setting a default hotkey
        if (toDefault)
        {
            _ctrl.Hotkey = new Hotkey(HotkeyControl.DefaultHotkeys[_ctrl.Name.Substring(2)]);
            goto SaveDirectly;
        }

        foreach (var hkInfo in Program.HotkeysList)
        {
            // Check if the user has cleared the hotkey field
            if (_ctrl.Hotkey == null)
            {
                Program.HotkeysList.FirstOrDefault(x => x.Command == ctrlName).Hotkey = null;
                _ctrl.FontStyle = FontStyles.Italic;
                break;
            }
            // Check if the received hotkey is not already assigned
            else if (_ctrl.Hotkey != null && hkInfo.Hotkey != null && hkInfo.Hotkey.ToString() == _ctrl.Hotkey.ToString() && hkInfo.Command != ctrlName)
            {
                _ctrl.Hotkey = _currentControlHotkey;
                _ctrl.FontStyle = _currentFontStyle;
                ShowLabel(Translate("InUse"));
                return;
            }
            // Check if the attempted hotkey is not restricted
            else if (HotkeyControl.RestrictedHotkeys.Where(x => _ctrl.Hotkey != null && x.Value.Equals(_ctrl.Hotkey.ToString())).Count() > 0)
            {
                _ctrl.Hotkey = _currentControlHotkey;
                _ctrl.FontStyle = _currentFontStyle;
                ShowLabel(Translate("Reserved"));
                return;
            }
            else
            {
                _ctrl.FontStyle = FontStyles.Normal;
                HideLabel();
            }
        }

    SaveDirectly:

        // Modify the XML document to update hotkey
        var document = new XmlDocument();
        document.Load(Constants.HotkeysFile);

        foreach (XmlNode entry in document.ChildNodes[0].ChildNodes)
        {
            if (entry.Name == ctrlName)
            {
                entry.InnerText = _ctrl.Hotkey == null ? "None" : _ctrl.Hotkey.ToString();
                document.Save(Constants.HotkeysFile);
                break;
            }
        }

        // Buffer new hotkey
        Program.HotkeysList.FirstOrDefault(x => x.Command == ctrlName).Hotkey = _ctrl.Hotkey;

        // Update InputGestureText of MenuItem
        foreach (var control in Program.MainWindow.MenuItems)
        {
            foreach (var item in control.Items)
            {
                if (item is MenuItem && (item as MenuItem).Name == $"MenuI_{ctrlName}")
                {
                    (item as MenuItem).InputGestureText = _ctrl.Hotkey == null ? "" : _ctrl.Hotkey.ToString();
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Show an error label with the specified message.
    /// </summary>
    /// <param name="message">The message to display in the label.</param>
    private void ShowLabel(string message)
    {
        LblDisallowed.Visibility = Visibility.Visible;
        LblDisallowed.Margin = new Thickness(_ctrl.Margin.Left + 140.0, _ctrl.Margin.Top, _ctrl.Margin.Right, _ctrl.Margin.Bottom);
        LblDisallowed.Content = message;
        LblDisallowed.Foreground = new SolidColorBrush(Colors.Red);
        LblDisallowed.FontWeight = FontWeights.Bold;
    }

    /// <summary>
    /// Hides the error label.
    /// </summary>
    private void HideLabel()
    {
        LblDisallowed.Visibility = Visibility.Collapsed;
    }
    #endregion
}