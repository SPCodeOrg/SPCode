using System;
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
        public static Config[] Configs;
        public static int SelectedConfig;

        public static UpdateInfo UpdateStatus;

        public static bool RCCKMade;
        public static DiscordRpcClient discordClient = new DiscordRpcClient(Constants.DiscordRPCAppID);
        public static Timestamps discordTime = Timestamps.Now;

        public static string Indentation => OptionsObject.Editor_ReplaceTabsToWhitespace
            ? new string(' ', OptionsObject.Editor_IndentationSize)
            : "\t";

        public static bool _IsLocalInstallation;


        [STAThread]
        public static void Main(string[] args)
        {
#if DEBUG     
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level =
                System.Diagnostics.SourceLevels.Critical;

#endif

            using (new Mutex(true, "SPCodeGlobalMutex", out var mutexReserved))
            {
                if (mutexReserved)
                {
#if !DEBUG
                    try
                    {
#endif

                    var splashScreen = new SplashScreen("Resources/Icon256x.png");
                    splashScreen.Show(false, true);
                    Environment.CurrentDirectory =
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ??
                        throw new NullReferenceException();
#if !DEBUG
                        ProfileOptimization.SetProfileRoot(Environment.CurrentDirectory);
                        ProfileOptimization.StartProfile("Startup.Profile");
#endif
                    _IsLocalInstallation = Paths.IsLocalInstallation();
                    UpdateStatus = new UpdateInfo();
                    OptionsObject = OptionsControlIOObject.Load(out var ProgramIsNew);

                    if (OptionsObject.Program_DiscordPresence)
                    {
                        // Init Discord RPC
                        discordClient.Initialize();

                        // Set default presence
                        discordClient.SetPresence(new RichPresence
                        {
                            State = "Idle",
                            Timestamps = discordTime,
                            Assets = new Assets
                            {
                                LargeImageKey = "immagine"
                            }
                        });
                    }


                    Translations = new TranslationProvider();
                    Translations.LoadLanguage(OptionsObject.Language, true);
                    foreach (var arg in args)
                    {
                        if (arg.ToLowerInvariant() == "-rcck") //ReCreateCryptoKey
                        {
                            OptionsObject.ReCreateCryptoKey();
                            MakeRCCKAlert();
                        }
                    }

                    Configs = ConfigLoader.Load();
                    for (var i = 0; i < Configs.Length; ++i)
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
                            if (Translations.AvailableLanguageIDs.Length > 0)
                            {
                                splashScreen.Close(new TimeSpan(0, 0, 1));
                                var languageWindow =
                                    new LanguageChooserWindow(Translations.AvailableLanguageIDs,
                                        Translations.AvailableLanguages);
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
                        File.WriteAllText($@"{Paths.GetCrashLogDirectory()}\CRASH_{Environment.TickCount}.txt",
                            BuildExceptionString(e, "SPCODE LOADING"));
                        MessageBox.Show(
                            "An error occured." + Environment.NewLine +
                            $"A crash report was written in {Paths.GetCrashLogDirectory()}",
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
                    OptionsControlIOObject.Save();
#if !DEBUG
                    }
                    catch (Exception e)
                    {
                        File.WriteAllText($@"{Paths.GetCrashLogDirectory()}\CRASH_{Environment.TickCount}.txt",
                            BuildExceptionString(e, "SPCODE MAIN"));
                        MessageBox.Show(
                            "An error occured." + Environment.NewLine +
                            $"A crash report was written in {Paths.GetCrashLogDirectory()}",
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

        private static string BuildExceptionString(Exception e, string SectionName)
        {
            var outString = new StringBuilder();
            outString.AppendLine("Section: " + SectionName);
            outString.AppendLine(".NET Version: " + Environment.Version);
            outString.AppendLine("Is local installation?: " + _IsLocalInstallation);
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
            for (; ; )
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