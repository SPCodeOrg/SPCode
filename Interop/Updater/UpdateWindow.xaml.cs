using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using Octokit;

namespace SPCode.Interop.Updater
{
    /// <summary>
    ///     Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow
    {
        private readonly UpdateInfo updateInfo;
        public bool Succeeded;

        public UpdateWindow()
        {
            InitializeComponent();
        }

        public UpdateWindow(UpdateInfo info)
        {
            updateInfo = info;
            InitializeComponent();
            DescriptionBox.Text = updateInfo.Release.Body;
            if (info.SkipDialog)
            {
                StartUpdate();
            }
        }

        private void ActionYesButton_Click(object sender, RoutedEventArgs e)
        {
            StartUpdate();
        }

        private void ActionNoButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
            Progress.IsActive = true;
            MainLine.Text = "Updating to " + updateInfo.Release.TagName;
            SubLine.Text = "Downloading Updater";
            var t = new Thread(UpdateDownloadWorker);
            t.Start();
        }

        private void UpdateDownloadWorker()
        {
#if DEBUG
            var asset = new ReleaseAsset("", 0, "", "SPCodeUpdater.exe", "", "", "", 0, 0, DateTimeOffset.Now,
                DateTimeOffset.Now, "https://hexah.net/SPCodeUpdater.exe", null);
#else
            var asset = updateInfo.Asset;
#endif
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

            Thread.Sleep(100); //safety reasons
            Dispatcher.Invoke(FinalizeUpdate);
        }

        private void FinalizeUpdate()
        {
            SubLine.Text = "Starting Updater";
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
    }
}