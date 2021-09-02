using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using MahApps.Metro;
using MdXaml;
using SPCode.Utils;

namespace SPCode.Interop.Updater
{
    public partial class UpdateWindow
    {
        #region Variables
        private readonly UpdateInfo updateInfo;
        public bool Succeeded;
        #endregion

        #region Constructors
        public UpdateWindow()
        {
            InitializeComponent();
        }

        public UpdateWindow(UpdateInfo info) : this()
        {

            if (Program.OptionsObject.Program_AccentColor != "Red" || Program.OptionsObject.Program_Theme != "BaseDark")
            {
                ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor),
                    ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme));
            }

            PrepareUpdateWindow(info);

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
        #endregion

        #region Methods
        public void PrepareUpdateWindow(UpdateInfo info)
        {
            Title = string.Format(Program.Translations.GetLanguage("VersionAvailable"), info.AllReleases[0].TagName);
            MainLine.Text = Program.Translations.GetLanguage("WantToUpdate");
            ActionYesButton.Content = Program.Translations.GetLanguage("Yes");
            ActionNoButton.Content = Program.Translations.GetLanguage("No");
            ActionGithubButton.Content = Program.Translations.GetLanguage("ViewGithub");

            var releasesBody = new StringBuilder();

            foreach (var release in info.AllReleases)
            {
                releasesBody.Append($"**%{{color:{GetAccentHex()}}}Version {release.TagName}%** ");
                releasesBody.AppendLine($"*%{{color:gray}}({release.CreatedAt.DateTime:MM/dd/yyyy})% *\r\n");
                releasesBody.AppendLine(release.Body + "\r\n");
            }

            releasesBody.Append($"*%{{color:gray}}More releases in {Constants.GitHubReleases}%*");

            var document = new Markdown();
            var content = document.Transform(releasesBody.ToString());
            content.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            DescriptionBox.Document = content;

            if (info.SkipDialog)
            {
                StartUpdate();
            }
        }

        private void StartUpdate()
        {
            if (updateInfo == null)
            {
                Close();
                return;
            }

            ActionYesButton.Visibility = Visibility.Hidden;
            ActionNoButton.Visibility = Visibility.Hidden;
            ActionGithubButton.Visibility = Visibility.Hidden;
            Icon.Visibility = Visibility.Hidden;
            MainLine.Text = string.Format(Program.Translations.GetLanguage("UpdatingTo"), updateInfo.AllReleases[0].TagName);
            SubLine.Text = Program.Translations.GetLanguage("DownloadingUpdater");
            var t = new Thread(UpdateDownloadWorker);
            t.Start();
        }

        private void UpdateDownloadWorker()
        {
            var asset = updateInfo.Asset;
            if (File.Exists(asset.Name))
            {
                File.Delete(asset.Name);
            }

            try
            {
                using var client = new WebClient();
                client.DownloadFile(asset.BrowserDownloadUrl, asset.Name);
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    "Error while downloading the updater." + Environment.NewLine + "Details: " + e.Message +
                    Environment.NewLine + "$$$" + e.StackTrace,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                Dispatcher.Invoke(Close);
            }

            Thread.Sleep(100);
            Dispatcher.Invoke(FinalizeUpdate);
        }

        private void FinalizeUpdate()
        {
            SubLine.Text = Program.Translations.GetLanguage("StartingUpdater");
            UpdateLayout();
            try
            {
                Process.Start(new ProcessStartInfo
                { Arguments = "/C SPCodeUpdater.exe", FileName = "cmd", WindowStyle = ProcessWindowStyle.Hidden });
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

        private string GetAccentHex()
        {
            return ThemeManager.DetectAppStyle(this).Item2.Resources["AccentColor"].ToString();
        }
        #endregion
    }
}