using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;

namespace SPCode.UI.Interop
{
    /// <summary>
    /// Interaction logic for LanguageChooserWindow.xaml
    /// </summary>
    public partial class LanguageChooserWindow : MetroWindow
    {
        public string SelectedID = string.Empty;
        public LanguageChooserWindow()
        {
            InitializeComponent();
        }

        public LanguageChooserWindow(string[] ids, string[] languages)
        {
            InitializeComponent();
            for (var i = 0; i < ids.Length; ++i)
            {
                LanguageBox.Items.Add(new ComboBoxItem() { Content = languages[i], Tag = ids[i] });
            }
            if (ids.Length > 0)
            {
                LanguageBox.SelectedIndex = 0;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var selectedObj = LanguageBox.SelectedItem;
            if (selectedObj == null)
            {
                return;
            }
            if (selectedObj is ComboBoxItem selectedItem)
            {
                SelectedID = (string)selectedItem.Tag;
            }
            Close();
        }
    }
}
