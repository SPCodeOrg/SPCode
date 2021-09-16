using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DiscordRPC;
using MahApps.Metro;
using SPCode.Interop;
using SPCode.Interop.Updater;
using SPCode.UI.Components;
using SPCode.Utils;
using Xceed.Wpf.AvalonDock;
using Xceed.Wpf.AvalonDock.Layout;
using Button = DiscordRPC.Button;

namespace SPCode.UI
{
    public partial class MainWindow
    {
        #region Variables
        private readonly Storyboard BlendOverEffect;
        private readonly Storyboard DisableServerAnim;
        public readonly List<EditorElement> EditorsReferences = new();
        public readonly List<DASMElement> DASMReferences = new();
        private readonly Storyboard EnableServerAnim;
        public readonly List<MenuItem> MenuItems;

        private readonly bool FullyInitialized;

        private ObservableCollection<string> ActionButtonDict = new()
        {
            Program.Translations.GetLanguage("Copy"),
            Program.Translations.GetLanguage("UploadFTP"),
            Program.Translations.GetLanguage("StartServer")
        };

        private ObservableCollection<string> CompileButtonDict = new()
        {
            Program.Translations.GetLanguage("CompileAll"),
            Program.Translations.GetLanguage("CompileCurrent")
        };
        #endregion

        #region Constructors
        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(SplashScreen sc)
        {
            InitializeComponent();
            if (Program.OptionsObject.Program_AccentColor != "Red" || Program.OptionsObject.Program_Theme != "BaseDark")
            {
                ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor),
                    ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme));
            }

            ObjectBrowserColumn.Width =
                new GridLength(Program.OptionsObject.Program_ObjectbrowserWidth, GridUnitType.Pixel);
            var heightDescriptor =
                DependencyPropertyDescriptor.FromProperty(ColumnDefinition.WidthProperty, typeof(ItemsControl));
            heightDescriptor.AddValueChanged(EditorObjectBrowserGrid.ColumnDefinitions[1],
                EditorObjectBrowserGridRow_WidthChanged);
            FillConfigMenu();
            CompileButton.ItemsSource = CompileButtonDict;
            CompileButton.SelectedIndex = 0;
            CActionButton.ItemsSource = ActionButtonDict;
            CActionButton.SelectedIndex = 0;

            if (Program.OptionsObject.UI_ShowToolBar)
            {
                Win_ToolBar.Height = double.NaN;
            }

            OBDirList.ItemsSource = Program.Configs[Program.SelectedConfig].SMDirectories;
            OBDirList.SelectedIndex = 0;

            MetroDialogOptions.AnimateHide = MetroDialogOptions.AnimateShow = false;
            BlendOverEffect = (Storyboard)Resources["BlendOverEffect"];
            EnableServerAnim = (Storyboard)Resources["EnableServerAnim"];
            DisableServerAnim = (Storyboard)Resources["DisableServerAnim"];
            ChangeObjectBrowserToDirectory(Program.OptionsObject.Program_ObjectBrowserDirectory);
            Language_Translate(true);

            if (Program.OptionsObject.LastOpenFiles != null)
            {
                foreach (var file in Program.OptionsObject.LastOpenFiles)
                {
                    TryLoadSourceFile(file, out _, false);
                }
            }

            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; ++i)
            {
                if (!args[i].EndsWith("exe"))
                {
                    TryLoadSourceFile(args[i], out _, false, true, i == 0);
                }
            }

            sc.Close(TimeSpan.FromMilliseconds(500.0));
            FullyInitialized = true;

            MenuItems = new()
            {
                MenuI_File,
                MenuI_Edit,
                MenuI_Build,
                MenuI_Tools,
                MenuI_Folding,
                MenuI_SPAPI,
                MenuI_Reformatter
            };

            LoadInputGestureTexts();
            LoadCommandsDictionary();
            LoadRecentsList();

            MenuI_ReopenLastClosedTab.IsEnabled = false;

            UpdateOBFileButton();

            SearchCooldownTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            SearchCooldownTimer.Tick += OnSearchCooldownTimerTick;

            LoggingControl.LogBox = LogTextbox;
        }
        #endregion

        #region Events
        private void DockingManager_ActiveContentChanged(object sender, EventArgs e)
        {
            if (OBTabFile.IsSelected)
            {
                ListViewOBItem_SelectFile(OBTabFile, null);
                OBTabFile.IsSelected = true;
            }

            UpdateWindowTitle();
            UpdateOBFileButton();
        }

        private void DockingManager_DocumentClosed(object sender, DocumentClosedEventArgs e)
        {
            (e.Document.Content as EditorElement)?.Close();
            (e.Document.Content as DASMElement)?.Close();
            UpdateWindowTitle();
            UpdateOBFileButton();
        }

        private void DockingPaneGroup_ChildrenTreeChanged(object sender, ChildrenTreeChangedEventArgs e)
        {
            // Luqs: Code taken from VisualPawn Editor (Not published yet)

            // if the active LayoutDocumentPane gets closed 
            // 1. it will not be in the LayoutDocumentPaneGroup.
            // 2. editor that get added to it will not be shown in the client.
            // Solution: Set the active LayoutDocumentPane to the first LayoutDocumentPaneGroup avilable child.

            if (e.Change == ChildrenTreeChange.DirectChildrenChanged
                && !DockingPaneGroup.Children.Contains(DockingPane)
                && DockingPaneGroup.Children.Count > 0
                && DockingPaneGroup.Children[0] is LayoutDocumentPane pane)
            {
                DockingPane = pane;
            }
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            if (ServerIsRunning)
            {
                ServerCheckThread.Abort();
                ServerProcess.Kill();
            }
            var lastOpenFiles = new List<string>();
            var editors = GetAllEditorElements();
            bool? SaveUnsaved = null;
            if (editors != null)
            {
                foreach (var editor in editors)
                {
                    if (File.Exists(editor.FullFilePath))
                    {
                        lastOpenFiles.Add(editor.FullFilePath);
                        if (editor.NeedsSave)
                        {
                            if (SaveUnsaved == null)
                            {
                                var result = MessageBox.Show(this, Program.Translations.GetLanguage("SavingUFiles"),
                                    Program.Translations.GetLanguage("Saving"), MessageBoxButton.YesNo,
                                    MessageBoxImage.Question);
                                SaveUnsaved = result == MessageBoxResult.Yes;
                            }

                            if (SaveUnsaved.Value)
                            {
                                editor.Close(true);
                            }
                            else
                            {
                                editor.Close(false, false);
                            }
                        }
                        else
                        {
                            editor.Close(false, false);
                        }
                    }
                }
            }

            Program.OptionsObject.LastOpenFiles = lastOpenFiles.ToArray();

            Program.DiscordClient.Dispose();
#if !DEBUG
            if (Program.UpdateStatus.IsAvailable)
            {
                var updateWin = new UpdateWindow(Program.UpdateStatus) { Owner = this };
                updateWin.ShowDialog();
            }
#endif
        }

        private void MetroWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                Activate();
                Focus();
                Debug.Assert(files != null, nameof(files) + " != null");
                for (var i = 0; i < files.Length; ++i)
                {
                    TryLoadSourceFile(files[i], out _, i == 0, true, i == 0);
                }
            }
        }



        private void EditorObjectBrowserGridRow_WidthChanged(object sender, EventArgs e)
        {
            if (FullyInitialized)
            {
                Program.OptionsObject.Program_ObjectbrowserWidth = ObjectBrowserColumn.Width.Value;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Loads a file into the editor.
        /// </summary>
        /// <param name="filePath">The path of the file to load</param>
        /// <param name="UseBlendoverEffect">Whether to execute the blendover effect</param>
        /// <param name="outEditor">The editor that has been loaded</param>
        /// <param name="TryOpenIncludes">Whether to open the includes associated with that file</param>
        /// <param name="SelectMe">Whether to focus the editor element once the file gets opened</param>
        /// <returns>If the file opening was successful or not</returns>
        public bool TryLoadSourceFile(string filePath, out EditorElement outEditor, bool UseBlendoverEffect = true, bool TryOpenIncludes = true, bool SelectMe = false)
        {
            outEditor = null;
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                if (fileInfo.Extension == ".sp" ||
                    fileInfo.Extension == ".inc" ||
                    fileInfo.Extension == ".txt" ||
                    fileInfo.Extension == ".cfg" ||
                    fileInfo.Extension == ".ini")
                {
                    var finalPath = fileInfo.FullName;
                    try
                    {
                        File.GetAccessControl(finalPath);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return false;
                    }

                    var editors = GetAllEditorElements();
                    if (editors != null)
                    {
                        foreach (var editor in editors)
                        {
                            if (editor.FullFilePath == finalPath)
                            {
                                if (SelectMe)
                                {
                                    editor.Parent.IsSelected = true;
                                }

                                outEditor = editor;
                                return true;
                            }
                        }
                    }

                    AddEditorElement(fileInfo, fileInfo.Name, SelectMe, out outEditor);
                    if (TryOpenIncludes && Program.OptionsObject.Program_OpenCustomIncludes)
                    {
                        using var textReader = fileInfo.OpenText();
                        var source = Regex.Replace(textReader.ReadToEnd(), @"/\*.*?\*/", string.Empty,
                            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline);
                        var regex = new Regex(@"^\s*\#include\s+((\<|"")(?<name>.+?)(\>|""))",
                            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline);
                        var mc = regex.Matches(source);
                        for (var i = 0; i < mc.Count; ++i)
                        {
                            try
                            {
                                var fileName = mc[i].Groups["name"].Value;
                                if (!(fileName.EndsWith(".inc", StringComparison.InvariantCultureIgnoreCase) ||
                                      fileName.EndsWith(".sp", StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    fileName += ".inc";
                                }

                                fileName = Path.Combine(
                                    fileInfo.DirectoryName ?? throw new NullReferenceException(), fileName);
                                TryLoadSourceFile(fileName, out _, false,
                                    Program.OptionsObject.Program_OpenIncludesRecursively);
                            }
                            catch (Exception)
                            {
                                // ignored
                            }
                        }
                    }
                }
                else if (fileInfo.Extension == ".smx")
                {
                    var viewers = GetAllDASMElements();
                    if (viewers != null)
                    {
                        foreach (var dasmviewer in viewers)
                        {
                            if (dasmviewer.FilePath == fileInfo.FullName)
                            {
                                DockingManager.ActiveContent = dasmviewer;
                                return true;
                            }
                        }
                    }
                    AddDASMElement(fileInfo);
                }

                if (UseBlendoverEffect)
                {
                    BlendOverEffect.Begin();
                }

                RefreshObjectBrowser();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a new editor element associated with the file to the Docking Manager.
        /// </summary>
        /// <param name="filePath">The path of the file</param>
        /// <param name="editorTitle">The title of the tab</param>
        /// <param name="SelectMe">Whether to focus this editor element once created.</param>
        private void AddEditorElement(FileInfo fInfo, string editorTitle, bool SelectMe, out EditorElement editor)
        {
            var layoutDocument = new LayoutDocument { Title = editorTitle };
            layoutDocument.ToolTip = fInfo.FullName;
            editor = new EditorElement(fInfo.FullName) { Parent = layoutDocument };
            layoutDocument.Content = editor;
            EditorsReferences.Add(editor);
            DockingPane.Children.Add(layoutDocument);
            AddNewRecentFile(fInfo);
            if (SelectMe)
            {
                layoutDocument.IsSelected = true;
                editor.editor.TextArea.Caret.Show();
            }
        }

        /// <summary>
        /// Adds a new DASM element associated with the file to the Docking Manager.
        /// </summary>
        private void AddDASMElement(FileInfo fileInfo)
        {
            var layoutDocument = new LayoutDocument { Title = "DASM: " + fileInfo.Name };
            var dasmElement = new DASMElement(fileInfo);
            DASMReferences.Add(dasmElement);
            layoutDocument.Content = dasmElement;
            DockingPane.Children.Add(layoutDocument);
            DockingPane.SelectedContentIndex = DockingPane.ChildrenCount - 1;
            AddNewRecentFile(fileInfo);
        }

        /// <summary>
        /// Performs a visual refresh on the editor.
        /// </summary>
        public static void ProcessUITasks()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(
                delegate
                {
                    frame.Continue = false;
                    return null;
                }), null);
            Dispatcher.PushFrame(frame);
        }

        /// <summary>
        /// Updates the editor's title and the Discord RPC status with the currently opened file.
        /// </summary>
        public void UpdateWindowTitle()
        {
            var ee = GetCurrentEditorElement();
            var de = GetCurrentDASMElement();
            var someEditorIsOpen = ee != null || de != null;
            var outString = "Idle";
            if (someEditorIsOpen)
            {
                outString = ee?.FullFilePath ?? de.FilePath;
            }

            outString += " - SPCode";

            if (Program.DiscordClient.IsInitialized)
            {
                var action = ee == null ? "Viewing" : "Editing";
                Program.DiscordClient.SetPresence(new RichPresence
                {
                    Timestamps = Program.OptionsObject.Program_DiscordPresenceTime ? Program.DiscordTime : null,
                    State = Program.OptionsObject.Program_DiscordPresenceFile ? someEditorIsOpen ? $"{action} {Path.GetFileName(ee?.FullFilePath ?? de.FilePath)}" : "Idle" : null,
                    Assets = new Assets
                    {
                        LargeImageKey = "immagine",
                    },
                    Buttons = new Button[]
                    {
                        new Button() { Label = Constants.GitHubRepositoryText, Url = Constants.GitHubRepository }
                    }
                });
            }

            if (ServerIsRunning)
            {
                outString = $"{outString} | {Program.Translations.GetLanguage("ServerRunning")}";
            }

            Title = outString;
        }
        #endregion
    }
}