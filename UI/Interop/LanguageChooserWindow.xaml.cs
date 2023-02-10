using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;

namespace SPCode.UI.Interop;

public partial class LanguageChooserWindow : MetroWindow
{
    public string SelectedID = string.Empty;
    public LanguageChooserWindow()
    {
        InitializeComponent();
    }

    public LanguageChooserWindow(List<string> ids, List<string> languages)
    {
        InitializeComponent();

        // Reorder English item to appear first
        languages.Remove("English");
        languages.Insert(0, "English");
        ids.Remove("default");
        ids.Insert(0, "default");

        for (var i = 0; i < ids.Count; i++)
        {
            LanguageBox.Items.Add(new ComboBoxItem
            {
                Content = languages[i],
                Tag = ids[i]
            });
        }
        if (ids.Count > 0)
        {
            LanguageBox.SelectedIndex = 0;
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (LanguageBox.SelectedItem is ComboBoxItem selectedItem)
        {
            SelectedID = (string)selectedItem.Tag;
        }
        Close();
    }
}