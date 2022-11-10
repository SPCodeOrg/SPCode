using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using DiscordRPC;
using ICSharpCode.AvalonEdit;
using MahApps.Metro;
using SPCode.Interop;
using SPCode.Interop.Updater;
using SPCode.UI;
using SPCode.UI.Interop;
using SPCode.Utils;

namespace SPCode
{
    public static class Program
    {
        public static MainWindow MainWindow;
        public static OptionsControl OptionsObject;
        public static TranslationProvider Translations;
        public static List<HotkeyInfo> HotkeysList;
        public static List<Config> Configs;
        public static int SelectedConfig;
        public static string SelectedTemplatePath;
        public static Stack<string> RecentFilesStack = new();
        public static UpdateInfo UpdateStatus;
        public static bool RCCKMade;
        public static DiscordRpcClient DiscordClient = new(Constants.DiscordRPCAppID);
        public static Timestamps DiscordTime = Timestamps.Now;
        public static bool PreventOptionsSaving = false;
        public static bool IsRTL;

        public static string Indentation => OptionsObject.Editor_ReplaceTabsToWhitespace
            ? new string(' ', OptionsObject.Editor_IndentationSize)
            : "\t";

        [STAThread]
        public static void Main(string[] args)
        {
            using (new Mutex(true, NamesHelper.MutexName, out var mutexReserved))
            {
                if (mutexReserved)
                {
#if !DEBUG
                    try
                    {
#endif
                        var splashScreen = new SplashScreen("Resources/Icons/icon256x.png");
                        splashScreen.Show(false, true);
                        Environment.CurrentDirectory =
                            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ??
                            throw new NullReferenceException();
#if !DEBUG
                        ProfileOptimization.SetProfileRoot(Environment.CurrentDirectory);
                        ProfileOptimization.StartProfile("Startup.Profile");
#endif
                        UpdateStatus = new UpdateInfo();
                        OptionsObject = OptionsControl.Load(out var ProgramIsNew);

                        if (!File.Exists(Constants.HotkeysFile))
                        {
                            HotkeyControl.CreateDefaultHotkeys();
                        }
                        else
                        {
                            HotkeyControl.CheckAndBufferHotkeys();
                        }

                        // Delete the default Ctrl+D hotkey to assign manually
                        AvalonEditCommands.DeleteLine.InputGestures.Clear();

                        if (OptionsObject.Program_DiscordPresence)
                        {
                            // Init Discord RPC
                            DiscordClient.Initialize();

                            // Set default presence
                            DiscordClient.SetPresence(new RichPresence
                            {
                                State = "Idle",
                                Timestamps = DiscordTime,
                                Assets = new Assets { LargeImageKey = "immagine" },
                                Buttons = new Button[]
                                {
                                    new Button()
                                    {
                                        Label = Constants.GetSPCodeText, Url = Constants.GitHubLatestRelease
                                    }
                                }
                            });
                        }

                        // Set up translations
                        Translations = new TranslationProvider();
                        Translations.LoadLanguage(OptionsObject.Language, true);

                        // Check startup arguments for -rcck
                        foreach (var arg in args)
                        {
                            if (arg.ToLowerInvariant() == "-rcck") //ReCreateCryptoKey
                            {
                                OptionsObject.ReCreateCryptoKey();
                                MakeRCCKAlert();
                            }
                        }

                        Configs = ConfigLoader.Load();
                        for (var i = 0; i < Configs.Count; ++i)
                        {
                            if (Configs[i].Name == OptionsObject.Program_SelectedConfig)
                            {
                                SelectedConfig = i;
                                break;
                            }
                        }

                        if (!OptionsObject.Program_UseHardwareAcceleration)
                        {
                            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
                        }
#if !DEBUG
                        if (ProgramIsNew)
                        {
                            if (Translations.AvailableLanguageIDs.Count > 0)
                            {
                                splashScreen.Close(new TimeSpan(0, 0, 1));
                                var langIds = Translations.AvailableLanguageIDs;
                                var langs = Translations.AvailableLanguages;
                                var languageWindow = new LanguageChooserWindow(langIds, langs);
                                languageWindow.ShowDialog();
                                var potentialSelectedLanguageID = languageWindow.SelectedID;
                                if (!string.IsNullOrWhiteSpace(potentialSelectedLanguageID))
                                {
                                    OptionsObject.Language = potentialSelectedLanguageID;
                                    Translations.LoadLanguage(potentialSelectedLanguageID);
                                }

                                splashScreen.Show(false, true);
                            }
                        }
#endif
                        MainWindow = new MainWindow(splashScreen);
                        var pipeServer = new PipeInteropServer(MainWindow);
                        pipeServer.Start();
#if !DEBUG
                    }
                    catch (Exception e)
                    {
                        var crashDir = PathsHelper.CrashLogDirectory;
                        File.WriteAllText($@"{crashDir}\CRASH_{Environment.TickCount}.txt",
                            BuildExceptionString(e, "SPCODE LOADING"));
                        MessageBox.Show(
                            "An error occured." + Environment.NewLine +
                            $"A crash report was written in {crashDir}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        Environment.Exit(Environment.ExitCode);
                    }
#endif
                    var app = new Application();
#if !DEBUG
                    try
                    {
                        if (OptionsObject.Program_CheckForUpdates)
                        {
                            Task.Run(UpdateCheck.Check);
                        }
#endif
                        app.Startup += App_Startup;
                        app.Run(MainWindow);
                        OptionsControl.Save();
#if !DEBUG
                    }
                    catch (Exception e)
                    {
                        var crashDir = PathsHelper.CrashLogDirectory;
                        File.WriteAllText($@"{crashDir}\CRASH_{Environment.TickCount}.txt",
                            BuildExceptionString(e, "SPCODE MAIN"));
                        MessageBox.Show(
                            "An error occured." + Environment.NewLine +
                            $"A crash report was written in {crashDir}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        Environment.Exit(Environment.ExitCode);
                    }
#endif
                }
                else
                {
                    try
                    {
                        var sBuilder = new StringBuilder();
                        var addedFiles = false;
                        for (var i = 0; i < args.Length; ++i)
                        {
                            if (!string.IsNullOrWhiteSpace(args[i]))
                            {
                                var fInfo = new FileInfo(args[i]);
                                if (fInfo.Exists)
                                {
                                    var ext = fInfo.Extension.ToLowerInvariant().Trim('.', ' ');
                                    if (ext == "sp" || ext == "inc" || ext == "txt" || ext == "smx")
                                    {
                                        addedFiles = true;
                                        sBuilder.Append(fInfo.FullName);
                                        if (i + 1 != args.Length)
                                        {
                                            sBuilder.Append("|");
                                        }
                                    }
                                }
                            }
                        }

                        if (addedFiles)
                        {
                            PipeInteropClient.ConnectToMasterPipeAndSendData(sBuilder.ToString());
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    } //dont fuck the user up with irrelevant data
                }
            }
        }

        public static void MakeRCCKAlert()
        {
            if (RCCKMade)
            {
                return;
            }

            RCCKMade = true;
            MessageBox.Show(
                "All FTP/RCon passwords are now encrypted wrong!" + Environment.NewLine + "You have to replace them!",
                "Created new crypto key", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void ClearUpdateFiles()
        {
            var files = Directory.GetFiles(Environment.CurrentDirectory, "*.exe", SearchOption.TopDirectoryOnly);
            for (var i = 0; i < files.Length; ++i)
            {
                var fInfo = new FileInfo(files[i]);
                if (fInfo.Name.StartsWith("updater_", StringComparison.CurrentCultureIgnoreCase))
                {
                    fInfo.Delete();
                }
            }
        }

        private static void App_Startup(object sender, StartupEventArgs e)
        {
            ThemeManager.DetectAppStyle(Application.Current);
            ThemeManager.ChangeAppStyle(Application.Current,
                ThemeManager.GetAccent("Green"),
                ThemeManager.GetAppTheme("BaseDark")); // or appStyle.Item1
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members")]
        private static string BuildExceptionString(Exception e, string SectionName)
        {
            var outString = new StringBuilder();
            outString.AppendLine("Section: " + SectionName);
            outString.AppendLine(".NET Version: " + Environment.Version);
            outString.AppendLine("Is local installation?: " + PathsHelper.LocalInstallation);
            outString.AppendLine("OS: " + Environment.OSVersion.VersionString);
            outString.AppendLine("64 bit OS: " + (Environment.Is64BitOperatingSystem ? "TRUE" : "FALSE"));
            outString.AppendLine("64 bit mode: " + (Environment.Is64BitProcess ? "TRUE" : "FALSE"));
            outString.AppendLine("Dir: " + Environment.CurrentDirectory);
            outString.AppendLine("Working Set: " + (Environment.WorkingSet / 1024) + " kb");
            outString.AppendLine("Installed UI Culture: " + CultureInfo.InstalledUICulture);
            outString.AppendLine("Current UI Culture: " + CultureInfo.CurrentUICulture);
            outString.AppendLine("Current Culture: " + CultureInfo.CurrentCulture);
            outString.AppendLine();
            var eNumber = 1;
            while (true)
            {
                if (e == null)
                {
                    break;
                }

                outString.AppendLine("Exception " + eNumber);
                outString.AppendLine("Message:");
                outString.AppendLine(e.Message);
                outString.AppendLine("Stacktrace:");
                outString.AppendLine(e.StackTrace);
                outString.AppendLine("Source:");
                outString.AppendLine(e.Source ?? "null");
                outString.AppendLine("HResult Code:");
                outString.AppendLine(e.HResult.ToString());
                outString.AppendLine("Helplink:");
                outString.AppendLine(e.HelpLink ?? "null");
                if (e.TargetSite != null)
                {
                    outString.AppendLine("Targetsite Name:");
                    outString.AppendLine(e.TargetSite.Name);
                }

                e = e.InnerException;
                eNumber++;
            }

            return eNumber - 1 + Environment.NewLine + outString;
        }
    }
}