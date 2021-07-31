using System.Windows;
using System.Windows.Controls;
using SPCode.UI.Components;
using SPCode.UI.Windows;

namespace SPCode.UI
{
    public partial class MainWindow
    {
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
            var editItem = new MenuItem() { Header = Program.Translations.GetLanguage("EditConfig") };
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
        }

        public void ChangeConfig(int index)
        {
            if (index < 0 || index >= Program.Configs.Length)
            {
                return;
            }
            Program.Configs[index].LoadSMDef();
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
            ObjectBrowserDirList.ItemsSource = Program.Configs[index].SMDirectories;
            ObjectBrowserDirList.Items.Refresh();
            ObjectBrowserDirList.SelectedIndex = 0;
        }

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
}
