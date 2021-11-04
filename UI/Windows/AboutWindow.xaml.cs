using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using MahApps.Metro;
using SPCode.Utils;

namespace SPCode.UI.Windows;

public partial class AboutWindow
{
    public AboutWindow()
    {
        InitializeComponent();
        Language_Translate();
        if (Program.OptionsObject.Program_AccentColor != "Red" || Program.OptionsObject.Program_Theme != "BaseDark")
        { ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor), ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme)); }

        Brush gridBrush = Program.OptionsObject.Program_Theme == "BaseDark" ?
          new SolidColorBrush(Color.FromArgb(0xC0, 0x10, 0x10, 0x10)) :
          new SolidColorBrush(Color.FromArgb(0xC0, 0xE0, 0xE0, 0xE0));
        gridBrush.Freeze();
        foreach (var c in ContentStackPanel.Children)
        {
            if (c is Grid g)
            {
                g.Background = gridBrush;
            }
        }
        TitleBox.Text = $"SPCode ({Assembly.GetEntryAssembly()?.GetName().Version}) - {Program.Translations.Get("SPEditCap")}";
        LicenseField.Text = File.ReadAllText(Constants.LicenseFile);
    }

    private void OpenLicenseFlyout(object sender, RoutedEventArgs e)
    {
        LicenseFlyout.IsOpen = true;
    }

    private void HyperlinkRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
        e.Handled = true;
    }

    private void Language_Translate()
    {
        if (Program.Translations.IsDefault)
        {
            return;
        }
        WrittenByBlock.Text = string.Format(Program.Translations.Get("WrittenBy"), "Julien Kluge (Julien.Kluge@gmail.com");
        LicenseBlock.Text = Program.Translations.Get("License");
        PeopleInvolvedBlock.Text = Program.Translations.Get("PeopleInv");
    }
}
