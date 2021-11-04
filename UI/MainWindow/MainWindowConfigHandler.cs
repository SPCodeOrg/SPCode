using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using SPCode.Interop;
using SPCode.UI.Components;
using SPCode.UI.Windows;

namespace SPCode.UI;

public partial class MainWindow
{
    /// <summary>
    /// Fills the config menu with all available configs.
    /// </summary>
    public void FillConfigMenu()
    {
        ConfigMenu.Items.Clear();
        for (var i = 0; i < Program.Configs.Length; ++i)
        {
            var item = new MenuItem
            {
                Header = Program.Configs[i].Name,
                IsCheckable = true,
                IsChecked = i == Program.SelectedConfig
            };
            item.Click += Item_Click;
            ConfigMenu.Items.Add(item);
        }
        ConfigMenu.Items.Add(new Separator());
        var editItem = new MenuItem() { Header = Program.Translations.Get("EditConfig") };
        editItem.Click += EditItem_Click;
        ConfigMenu.Items.Add(editItem);
    }

    private void EditItem_Click(object sender, RoutedEventArgs e)
    {
        var configWindow = new ConfigWindow() { Owner = this, ShowInTaskbar = false };
        configWindow.ShowDialog();
    }

    private void Item_Click(object sender, RoutedEventArgs e)
    {
        var name = (string)((MenuItem)sender).Header;
        ChangeConfig(name);
        LoggingControl.LogAction($"Changed to config \"{name}\".", 2);
    }

    /// <summary>
    /// Changes to the specified config.
    /// </summary>
    /// <param name="index">The config index to change to</param>
    public void ChangeConfig(int index)
    {
        if (index < 0 || index >= Program.Configs.Length)
        {
            return;
        }

        Program.Configs[index].LoadSMDef();

        if (Program.Configs[index].RejectedPaths.Any())
        {
            var sb = new StringBuilder();
            sb.Append("SPCode was unauthorized to access the following directories to parse their includes: \n");
            foreach (var path in Program.Configs[index].RejectedPaths)
            {
                sb.Append($"  - {path}\n");
            }

            LoggingControl.LogAction(sb.ToString());
        }

        var name = Program.Configs[index].Name;
        for (var i = 0; i < ConfigMenu.Items.Count - 2; ++i)
        {
            ((MenuItem)ConfigMenu.Items[i]).IsChecked = name == (string)((MenuItem)ConfigMenu.Items[i]).Header;
        }

        Program.SelectedConfig = index;
        Program.OptionsObject.Program_SelectedConfig = Program.Configs[Program.SelectedConfig].Name;

        var editors = GetAllEditorElements();
        if (editors != null)
        {
            foreach (var editor in editors)
            {
                editor.LoadAutoCompletes();
                editor.editor.SyntaxHighlighting = new AeonEditorHighlighting();
                editor.InvalidateVisual();
            }
        }

        OBDirList.ItemsSource = Program.Configs[index].SMDirectories;
        OBDirList.Items.Refresh();
        OBDirList.SelectedIndex = 0;
    }

    /// <summary>
    /// Overload of ChangeConfig to take the name of the config.
    /// </summary>
    /// <param name="name">Name of the config to change to.</param>
    private void ChangeConfig(string name)
    {
        for (var i = 0; i < Program.Configs.Length; ++i)
        {
            if (Program.Configs[i].Name == name)
            {
                ChangeConfig(i);
                return;
            }
        }
    }

}
