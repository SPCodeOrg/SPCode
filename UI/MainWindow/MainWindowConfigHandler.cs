using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MahApps.Metro.Controls;
using SPCode.Interop;
using SPCode.UI.Components;
using SPCode.UI.Windows;
using static SPCode.Interop.TranslationProvider;

namespace SPCode.UI;

public partial class MainWindow
{
    /// <summary>
    /// Fills the config menu with all available configs.
    /// </summary>
    public void FillConfigMenu()
    {
        ConfigMenu.Items.Clear();
        for (var i = 0; i < Program.Configs.Count; ++i)
        {
            var item = new MenuItem
            {
                Header = Program.Configs[i].Name, IsCheckable = true, IsChecked = i == Program.SelectedConfig
            };
            item.Click += Item_Click;
            ConfigMenu.Items.Add(item);
        }

        ConfigMenu.Items.Add(new Separator());
        var editItem = new MenuItem() { Header = Translate("EditConfig") };
        editItem.Click += EditItem_Click;
        ConfigMenu.Items.Add(editItem);
    }

    private void EditItem_Click(object sender, RoutedEventArgs e)
    {
        var configWindow = new ConfigWindow() { Owner = this, ShowInTaskbar = false };
        DimmMainWindow();
        configWindow.ShowDialog();
        RestoreMainWindow();
    }

    private async void Item_Click(object sender, RoutedEventArgs e)
    {
        var name = (string)((MenuItem)sender).Header;
        await ChangeConfig(name);
        LoggingControl.LogAction($"Changed to config \"{name}\".", 2);
    }

    /// <summary>
    /// Changes to the specified config.
    /// </summary>
    /// <param name="index">The config index to change to</param>
    public async Task ChangeConfig(int index)
    {
        if (index < 0 || index >= Program.Configs.Count)
        {
            return;
        }
        var config = Program.Configs[index];
        
        await Task.Run(() =>
        {
            config.LoadSMDef();
        });
        
        this.Invoke(() =>
        {
            if (config.RejectedPaths.Any())
            {
                var sb = new StringBuilder();
                sb.Append(
                    "SPCode was unauthorized to access the following directories to parse their includes: \n");
                foreach (var path in config.RejectedPaths)
                {
                    sb.Append($"  - {path}\n");
                }

                LoggingControl.LogAction(sb.ToString());
            }

            var name = config.Name;
            for (var i = 0; i < ConfigMenu.Items.Count - 2; ++i)
            {
                var item = (MenuItem)ConfigMenu.Items[i];
                item.IsChecked = name == (string)item.Header;
            }

            Program.SelectedConfig = index;
            Program.OptionsObject.Program_SelectedConfig = config.Name;
            
            if (EditorReferences.Any())
            {
                foreach (var editor in EditorReferences)
                {
                    editor.LoadAutoCompletes();
                    editor.editor.SyntaxHighlighting = new AeonEditorHighlighting();
                    editor.InvalidateVisual();
                }
            }
            
            OBDirList.ItemsSource = config.SMDirectories; // very slow
            OBDirList.Items.Refresh();
            OBDirList.SelectedIndex = 0;
        });
    }

    /// <summary>
    /// Overload of ChangeConfig to take the name of the config.
    /// </summary>
    /// <param name="name">Name of the config to change to.</param>
    private async Task ChangeConfig(string name)
    {
        for (var i = 0; i < Program.Configs.Count; ++i)
        {
            if (Program.Configs[i].Name == name)
            {
                await ChangeConfig(i);
                return;
            }
        }
    }
}