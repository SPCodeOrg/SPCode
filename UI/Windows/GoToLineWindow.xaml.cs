using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro;

namespace SPCode.UI.Windows
{
    public partial class GoToLineWindow
    {
        public GoToLineWindow()
        {
            InitializeComponent();
            if (Program.OptionsObject.Program_AccentColor != "Red" || Program.OptionsObject.Program_Theme != "BaseDark")
            {
                ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor),
                    ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme));
            }
        }
        private void JumpNumberKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void JumpToNumber(object sender, RoutedEventArgs e)
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
