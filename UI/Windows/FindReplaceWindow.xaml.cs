using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro;

namespace SPCode.UI.Windows
{
    public partial class FindReplaceWindow
    {
        public FindReplaceWindow()
        {
            InitializeComponent();
            if (Program.OptionsObject.Program_AccentColor != "Red" || Program.OptionsObject.Program_Theme != "BaseDark")
            {
                ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor),
                    ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme));
            }
        }

        private void FindReplaceGrid_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void SearchBoxKeyUp(object sender, KeyEventArgs e)
        {

        }

        private void SearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void CloseFindReplaceGrid(object sender, RoutedEventArgs e)
        {

        }

        private void ReplaceBoxKeyUp(object sender, KeyEventArgs e)
        {

        }

        private void SearchButtonClicked(object sender, RoutedEventArgs e)
        {

        }

        private void ReplaceButtonClicked(object sender, RoutedEventArgs e)
        {

        }

        private void CountButtonClicked(object sender, RoutedEventArgs e)
        {

        }

        private void MetroWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
