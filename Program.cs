using Spcode.Interop;
using Spcode.Interop.Updater;
using Spcode.UI;
using System;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using DiscordRPC;
using Application = System.Windows.Application;

namespace Spcode
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
		public static readonly DiscordRpcClient discordClient = new DiscordRpcClient("692110664948514836");
		public static Timestamps discordTime = Timestamps.Now;
		
        [STAThread]
        public static async Task Main(string[] args)
        {
	        // Init Discord RPC
	        discordClient.Initialize();
	        
	        // Set default presence
	        discordClient.SetPresence(new RichPresence()
	        {
		        State = "Idle",
		        Timestamps = discordTime,
		        Assets = new Assets()
		        {
			        LargeImageKey = "immagine",
		        }
	        });
	        bool mutexReserved;
            using (new Mutex(true, "SpcodeGlobalMutex", out mutexReserved))
            {
	            if (mutexReserved)
	            {
#if !DEBUG
		            try
		            {
#endif
			            SplashScreen splashScreen = new SplashScreen("Resources/Icon256x.png");
			            splashScreen.Show(false, true);
			            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new NullReferenceException();
#if !DEBUG
			            ProfileOptimization.SetProfileRoot(Environment.CurrentDirectory);
			            ProfileOptimization.StartProfile("Startup.Profile");
#endif
			            UpdateStatus = new UpdateInfo();
			            OptionsObject = OptionsControlIOObject.Load(out var ProgramIsNew);
			            Translations = new TranslationProvider();
			            Translations.LoadLanguage(OptionsObject.Language, true);
			            for (int i = 0; i < args.Length; ++i)
			            {
				            if (args[i].ToLowerInvariant() == "-rcck") //ReCreateCryptoKey
				            {
					            OptionsObject.ReCreateCryptoKey();
					            MakeRCCKAlert();
				            }
			            }
			            Configs = ConfigLoader.Load();
			            for (int i = 0; i < Configs.Length; ++i)
			            {
				            if (Configs[i].Name == OptionsObject.Program_SelectedConfig)
				            {
					            SelectedConfig = i;
					            break;
				            }
			            }
			            if (!OptionsObject.Program_UseHardwareAcceleration)
			            {
				            RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
			            }
#if !DEBUG
			            if (ProgramIsNew)
			            {
				            if (Translations.AvailableLanguageIDs.Length > 0)
				            {
					            splashScreen.Close(new TimeSpan(0, 0, 1));
					            var languageWindow = new UI.Interop.LanguageChooserWindow(Translations.AvailableLanguageIDs, Translations.AvailableLanguages);
					            languageWindow.ShowDialog();
					            string potentialSelectedLanguageID = languageWindow.SelectedID;
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
			            PipeInteropServer pipeServer = new PipeInteropServer(MainWindow);
			            pipeServer.Start();
#if !DEBUG
		            }
		            catch (Exception e)
		            {
			            File.WriteAllText("CRASH_" + Environment.TickCount.ToString() + ".txt", BuildExceptionString(e, "SPCODE LOADING"));
			            MessageBox.Show("An error occured while loading." + Environment.NewLine + "A crash report was written in the editor-directory.",
				            "Error while Loading",
				            MessageBoxButton.OK,
				            MessageBoxImage.Error);
			            Environment.Exit(Environment.ExitCode);
		            }
#endif
		            Application app = new Application();
#if !DEBUG
		            try
		            {
			            if (OptionsObject.Program_CheckForUpdates)
			            {
				            await UpdateCheck.Check();
			            }
#endif
			            app.Startup += App_Startup;
			            app.Run(MainWindow);
			            OptionsControlIOObject.Save();
#if !DEBUG
		            }
		            catch (Exception e)
		            {
			            File.WriteAllText("CRASH_" + Environment.TickCount.ToString() + ".txt", BuildExceptionString(e, "SPCODE MAIN"));
			            MessageBox.Show("An error occured." + Environment.NewLine + "A crash report was written in the editor-directory.",
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
			            StringBuilder sBuilder = new StringBuilder();
			            bool addedFiles = false;
			            for (int i = 0; i < args.Length; ++i)
			            {
				            if (!string.IsNullOrWhiteSpace(args[i]))
				            {
					            FileInfo fInfo = new FileInfo(args[i]);
					            if (fInfo.Exists)
					            {
						            string ext = fInfo.Extension.ToLowerInvariant().Trim(new char[] { '.', ' ' });
						            if (ext == "sp" || ext == "inc" || ext == "txt" || ext == "smx")
						            {
							            addedFiles = true;
							            sBuilder.Append(fInfo.FullName);
							            if ((i + 1) != args.Length)
							            { sBuilder.Append("|"); }
						            }
					            }
				            }
			            }
			            if (addedFiles)
			            { PipeInteropClient.ConnectToMasterPipeAndSendData(sBuilder.ToString()); }
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
			MessageBox.Show("All FTP/RCon passwords are now encrypted wrong!" + Environment.NewLine + "You have to replace them!",
				"Created new crypto key", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		public static void ClearUpdateFiles()
		{
			string[] files = Directory.GetFiles(Environment.CurrentDirectory, "*.exe", SearchOption.TopDirectoryOnly);
			for (int i = 0; i < files.Length; ++i)
			{
				FileInfo fInfo = new FileInfo(files[i]);
				if (fInfo.Name.StartsWith("updater_", StringComparison.CurrentCultureIgnoreCase))
				{
					fInfo.Delete();
				}
			}
		}

		public static string Indentation => OptionsObject.Editor_ReplaceTabsToWhitespace ? new string(' ', OptionsObject.Editor_IndentationSize) : "\t";

		private static void App_Startup(object sender, StartupEventArgs e)
		{
			MahApps.Metro.ThemeManager.DetectAppStyle(Application.Current);
			MahApps.Metro.ThemeManager.ChangeAppStyle(Application.Current,
									MahApps.Metro.ThemeManager.GetAccent("Green"),
									MahApps.Metro.ThemeManager.GetAppTheme("BaseDark")); // or appStyle.Item1
		}

		private static string BuildExceptionString(Exception e, string SectionName)
        {
            StringBuilder outString = new StringBuilder();
            outString.AppendLine("Section: " + SectionName);
            outString.AppendLine(".NET Version: " + Environment.Version);
            outString.AppendLine("OS: " + Environment.OSVersion.VersionString);
            outString.AppendLine("64 bit OS: " + ((Environment.Is64BitOperatingSystem) ? "TRUE" : "FALSE"));
            outString.AppendLine("64 bit mode: " + ((Environment.Is64BitProcess) ? "TRUE" : "FALSE"));
            outString.AppendLine("Dir: " + Environment.CurrentDirectory);
            outString.AppendLine("Working Set: " + (Environment.WorkingSet / 1024).ToString() + " kb");
            outString.AppendLine("Installed UI Culture: " + System.Globalization.CultureInfo.InstalledUICulture);
            outString.AppendLine("Current UI Culture: " + System.Globalization.CultureInfo.CurrentUICulture);
            outString.AppendLine("Current Culture: " + System.Globalization.CultureInfo.CurrentCulture);
            outString.AppendLine();
            int eNumber = 1;
            for (; ; )
            {
                if (e == null)
                {
                    break;
                }
                outString.AppendLine("Exception " + eNumber.ToString());
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
            return (eNumber - 1) + Environment.NewLine + outString;
        }
    }
}
