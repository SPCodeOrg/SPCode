using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
using MdXaml;
using SPCode.Utils;
using static SPCode.Interop.TranslationProvider;

namespace SPCode.Interop.Updater;

public partial class UpdateWindow
{
    #region Variables
    private readonly UpdateInfo _updateInfo;
    public bool Succeeded;
    #endregion

    #region Constructors
    public UpdateWindow()
    {
        InitializeComponent();
    }

    public UpdateWindow(UpdateInfo info, bool OnlyChangelog = false) : this()
    {

        if (Program.OptionsObject.Program_AccentColor != "Red" || Program.OptionsObject.Program_Theme != "BaseDark")
        {
            ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor),
                ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme));
        }

        _updateInfo = info;
        PrepareUpdateWindow(OnlyChangelog);

    }
    #endregion

    #region Events
    private void ActionYesButton_Click(object sender, RoutedEventArgs e)
    {
        StartUpdate();
    }

    private void ActionNoButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ActionGithubButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(Constants.GitHubLatestRelease));
    }

    private void MetroWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }
    #endregion

    #region Methods
    /// <summary>
    /// Prepares the update window with all the necessary info.
    /// </summary>
    public void PrepareUpdateWindow(bool OnlyChangelog = false)
    {
        if (OnlyChangelog)
        {
            Title = "SPCode Changelog";
            MainLine.Visibility = Visibility.Hidden;
            ActionYesButton.Visibility = Visibility.Hidden;
            ActionNoButton.Visibility = Visibility.Hidden;
            ActionGithubButton.Visibility = Visibility.Hidden;
            DescriptionBox.Margin = new Thickness(0, 0, 0, 0);
        }
        else
        {
            Title = string.Format(Translate("VersionAvailable"), _updateInfo.AllReleases[0].TagName);
            MainLine.Text = string.Format(Translate("WantToUpdate"), NamesHelper.VersionString, _updateInfo.AllReleases[0].TagName);
            ActionYesButton.Content = Translate("Yes");
            ActionNoButton.Content = Translate("No");
            ActionGithubButton.Content = Translate("ViewGithub");
        }

        var releasesBody = new StringBuilder();

        if (_updateInfo.AllReleases != null && _updateInfo.AllReleases.Count > 0)
        {
            foreach (var release in _updateInfo.AllReleases)
            {
                releasesBody.Append($"**%{{color:{GetAccentHex()}}}Version {release.TagName}%** ");
                releasesBody.AppendLine($"*%{{color:gray}}({MonthToTitlecase(release.CreatedAt)})% *\r\n");
                releasesBody.AppendLine(release.Body + "\r\n");
            }
        }

        releasesBody.Append($"*%{{color:gray}}More releases in {Constants.GitHubReleases}%*");

        var document = new Markdown();
        var content = document.Transform(releasesBody.ToString());
        content.FontFamily = new FontFamily("Segoe UI");
        DescriptionBox.Document = content;

        if (_updateInfo.SkipDialog)
        {
            StartUpdate();
        }
    }

    /// <summary>
    /// Triggers the update process
    /// </summary>
    private void StartUpdate()
    {
        if (_updateInfo == null)
        {
            Close();
            return;
        }

        ActionYesButton.Visibility = Visibility.Hidden;
        ActionNoButton.Visibility = Visibility.Hidden;
        ActionGithubButton.Visibility = Visibility.Hidden;
        MainLine.Text = string.Format(Translate("UpdatingTo"), _updateInfo.AllReleases[0].TagName);
        SubLine.Text = Translate("DownloadingUpdater");
        var t = new Thread(UpdateDownloadWorker);
        t.Start();
    }

    /// <summary>
    /// Download worker in charge of downloading the updater asset.
    /// </summary>
    private void UpdateDownloadWorker()
    {
        var updater = _updateInfo.Updater;
        var portable = _updateInfo.Portable;

        try
        {
            if (File.Exists(updater.Name))
            {
                File.Delete(updater.Name);
            }

            if (File.Exists(portable.Name))
            {
                File.Delete(portable.Name);
            }
            using var client = new WebClient();
            client.DownloadFile(updater.BrowserDownloadUrl, updater.Name);
            client.DownloadFile(portable.BrowserDownloadUrl, portable.Name);
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                Program.MainWindow.ShowMessageAsync("Error while downloading the update assets",
                    $"{ex.Message}", MessageDialogStyle.Affirmative, Program.MainWindow.MetroDialogOptions);
                Close();
            });
            return;
        }

        Thread.Sleep(100);
        Dispatcher.Invoke(FinalizeUpdate);
    }

    /// <summary>
    /// Dowload finalized callback
    /// </summary>
    private void FinalizeUpdate()
    {
        SubLine.Text = Translate("StartingUpdater");
        UpdateLayout();
        try
        {
            Process.Start(new ProcessStartInfo
            {
                Arguments = "/C SPCodeUpdater.exe",
                FileName = "cmd",
                WindowStyle = ProcessWindowStyle.Hidden
            });
            Succeeded = true;
        }
        catch (Exception e)
        {
            MessageBox.Show(
                "Error while trying to start the updater." + Environment.NewLine + "Details: " + e.Message +
                Environment.NewLine + "$$$" + e.StackTrace,
                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        Close();
    }

    /// <summary>
    /// Transforms the current accent color into the hex color format.
    /// </summary>
    /// <returns></returns>
    private string GetAccentHex()
    {
        return ThemeManager.DetectAppStyle(this).Item2.Resources["AccentColor"].ToString();
    }

    /// <summary>
    /// Returns the specified DateTimeOffset into a MMMM dd, yyyy with the month's initial letter in uppercase.
    /// </summary>
    /// <param name="dateOff"></param>
    /// <returns></returns>
    private static string MonthToTitlecase(DateTimeOffset dateOff)
    {
        var date = dateOff.DateTime.ToString("MMMM dd, yyyy", CultureInfo.GetCultureInfo("en-US"));
        return char.ToUpper(date[0]) + date.Substring(1);
    }
    #endregion
}