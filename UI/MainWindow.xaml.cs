using MahApps.Metro;
using Spedit.UI.Components;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DiscordRPC;
using Spedit.Interop.Updater;
using Xceed.Wpf.AvalonDock.Layout;
// using Spedit.Interop.Updater; //not delete! ?

namespace Spedit.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public readonly List<EditorElement> EditorsReferences = new List<EditorElement>();

        readonly Storyboard BlendOverEffect;
        readonly Storyboard FadeFindReplaceGridIn;
        readonly Storyboard FadeFindReplaceGridOut;
        readonly Storyboard EnableServerAnim;
        readonly Storyboard DisableServerAnim;

		private readonly bool FullyInitialized;

        public MainWindow()
        {
            InitializeComponent();
        }
        public MainWindow(SplashScreen sc)
        {
            InitializeComponent();
			if (Program.OptionsObject.Program_AccentColor != "Red" || Program.OptionsObject.Program_Theme != "BaseDark")
			{ ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor), ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme)); }
			ObjectBrowserColumn.Width = new GridLength(Program.OptionsObject.Program_ObjectbrowserWidth, GridUnitType.Pixel);
			var heightDescriptor = DependencyPropertyDescriptor.FromProperty(ColumnDefinition.WidthProperty, typeof(ItemsControl));
			heightDescriptor.AddValueChanged(EditorObjectBrowserGrid.ColumnDefinitions[1], EditorObjectBrowserGridRow_WidthChanged);
			FillConfigMenu();
            CompileButton.ItemsSource = compileButtonDict;
			CompileButton.SelectedIndex = 0;
            CActionButton.ItemsSource = actionButtonDict;
			CActionButton.SelectedIndex = 0;
            ReplaceButton.ItemsSource = findReplaceButtonDict;
			ReplaceButton.SelectedIndex = 0;
            if (Program.OptionsObject.UI_ShowToolBar)
            {
                Win_ToolBar.Height = double.NaN;
            }
            MetroDialogOptions.AnimateHide = MetroDialogOptions.AnimateShow = false;
            BlendOverEffect = (Storyboard)Resources["BlendOverEffect"];
            FadeFindReplaceGridIn = (Storyboard)Resources["FadeFindReplaceGridIn"];
            FadeFindReplaceGridOut = (Storyboard)Resources["FadeFindReplaceGridOut"];
            EnableServerAnim = (Storyboard)Resources["EnableServerAnim"];
            DisableServerAnim = (Storyboard)Resources["DisableServerAnim"];
			ChangeObjectBrowserToDirectory(Program.OptionsObject.Program_ObjectBrowserDirectory);
			Language_Translate(true);
#if DEBUG
            TryLoadSourceFile(@"C:\Users\Jelle\Desktop\scripting\AeroControler.sp", false);
#endif
            if (Program.OptionsObject.LastOpenFiles != null)
            {
                foreach (var file in Program.OptionsObject.LastOpenFiles)
                {
                    TryLoadSourceFile(file, false);
                }
            }
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; ++i)
            {
                if (!args[i].EndsWith("exe"))
                {
                    TryLoadSourceFile(args[i], false, true, (i == 0));
                }
            }
            sc.Close(TimeSpan.FromMilliseconds(500.0));
			StartBackgroundParserThread();
			FullyInitialized = true;
		}

        public bool TryLoadSourceFile(string filePath, bool UseBlendoverEffect = true, bool TryOpenIncludes = true, bool SelectMe = false)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                string extension = fileInfo.Extension.ToLowerInvariant().Trim('.', ' ');
                if (extension == "sp" || extension == "inc" || extension == "txt" || extension == "cfg" || extension == "ini")
                {
                    string finalPath = fileInfo.FullName;
                    try
                    {
                        File.GetAccessControl(finalPath);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return false;
                    }
                    EditorElement[] editors = GetAllEditorElements();
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
                                return true;
                            }
                        }
                    }
                    AddEditorElement(finalPath, fileInfo.Name, SelectMe);
                    if (TryOpenIncludes && Program.OptionsObject.Program_OpenCustomIncludes)
                    {
                        using (var textReader = fileInfo.OpenText())
                        {
                            string source = Regex.Replace(textReader.ReadToEnd(), @"/\*.*?\*/", string.Empty, RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline);
                            Regex regex = new Regex(@"^\s*\#include\s+((\<|"")(?<name>.+?)(\>|""))", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline);
                            MatchCollection mc = regex.Matches(source);
                            for (int i = 0; i < mc.Count; ++i)
                            {
                                try
                                {
                                    string fileName = mc[i].Groups["name"].Value;
                                    if (!(fileName.EndsWith(".inc", StringComparison.InvariantCultureIgnoreCase) || fileName.EndsWith(".sp", StringComparison.InvariantCultureIgnoreCase)))
                                    {
                                        fileName = fileName + ".inc";
                                    }
                                    fileName = Path.Combine(fileInfo.DirectoryName ?? throw new NullReferenceException(), fileName);
                                    TryLoadSourceFile(fileName, false, Program.OptionsObject.Program_OpenIncludesRecursively);
                                }
                                catch (Exception)
                                {
                                    // ignored
                                }
                            }
                        }
                    }
                }
                else if (extension == "smx")
                {
                    LayoutDocument layoutDocument = new LayoutDocument {Title = "DASM: " + fileInfo.Name};
                    DASMElement dasmElement = new DASMElement(fileInfo);
                    layoutDocument.Content = dasmElement;
                    DockingPane.Children.Add(layoutDocument);
                    DockingPane.SelectedContentIndex = DockingPane.ChildrenCount - 1;
                }
                if (UseBlendoverEffect)
                {
                    BlendOverEffect.Begin();
                }
                return true;
            }
            return false;
        }

        private void AddEditorElement(string filePath, string name, bool SelectMe)
        {
            LayoutDocument layoutDocument = new LayoutDocument {Title = name};
            layoutDocument.Closing += layoutDocument_Closing;
            layoutDocument.ToolTip = filePath;
            EditorElement editor = new EditorElement(filePath) {Parent = layoutDocument};
            layoutDocument.Content = editor;
            EditorsReferences.Add(editor);
            DockingPane.Children.Add(layoutDocument);
            if (SelectMe)
            {
				layoutDocument.IsSelected = true;
            }
        }

        private void DockingManager_ActiveContentChanged(object sender, EventArgs e)
        {
            UpdateWindowTitle();
            EditorElement ee = GetCurrentEditorElement();
            if (ee != null)
            {
                ee.editor.Focus();
                Program.discordClient.SetPresence(new RichPresence()
                {
                    Timestamps = Program.discordTime,
                    State = $"Editing {Path.GetFileName(ee.FullFilePath)}",
                    Assets = new Assets()
                    {
                        LargeImageKey = "immagine",
                        LargeImageText = $"Editing {Path.GetFileName(ee.FullFilePath)}",
                    }
                });
            }
        }

        private void DockingManager_DocumentClosing(object sender, Xceed.Wpf.AvalonDock.DocumentClosingEventArgs e)
        {
            ((EditorElement) e.Document.Content).Close();
            UpdateWindowTitle();
        }

        private void layoutDocument_Closing(object sender, CancelEventArgs e)
        {
            // e.Cancel = true;
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            backgroundParserThread?.Abort();
            parseDistributorTimer?.Stop();
            ServerCheckThread?.Abort(); //a join would not work, so we have to be..forcefully...
            List<string> lastOpenFiles = new List<string>();
            EditorElement[] editors = GetAllEditorElements();
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
                                var result = MessageBox.Show(this, Program.Translations.GetLanguage("SavingUFiles"), Program.Translations.GetLanguage("Saving"), MessageBoxButton.YesNo, MessageBoxImage.Question);
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
#if !DEBUG
            if (Program.UpdateStatus.IsAvailable)
            {
                UpdateWindow updateWin = new UpdateWindow(Program.UpdateStatus) { Owner = this };
                updateWin.ShowDialog();
            }
#endif
        }

        private void MetroWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                Activate();
                Focus();
                Debug.Assert(files != null, nameof(files) + " != null");
                for (int i = 0; i < files.Length; ++i)
                {
                    TryLoadSourceFile(files[i], (i == 0), true, (i == 0));
                }
            }
        }

        private static void ProcessUITasks()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
        }

        private void ErrorResultGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var row = ((ErrorDataGridRow)ErrorResultGrid.SelectedItem);
            if (row == null)
            {
                return;
            }
            string fileName = row.file;
            EditorElement[] editors = GetAllEditorElements();
            if (editors == null)
            {
                return;
            }
            foreach (var editor in editors)
            {
                if (editor.FullFilePath == fileName)
                {
                    editor.Parent.IsSelected = true;
                    int line = GetLineInteger(row.line);
                    if (line > 0 && line <= editor.editor.LineCount)
                    {
                        var lineObj = editor.editor.Document.Lines[line - 1];
                        editor.editor.ScrollToLine(line - 1);
                        editor.editor.Select(lineObj.Offset, lineObj.Length);
                    }
                }
            }
        }

        private void CloseErrorResultGrid(object sender, RoutedEventArgs e)
        {
            CompileOutputRow.Height = new GridLength(8.0);
        }

		private void EditorObjectBrowserGridRow_WidthChanged(object sender, EventArgs e)
		{
			if (FullyInitialized)
			{
				Program.OptionsObject.Program_ObjectbrowserWidth = ObjectBrowserColumn.Width.Value;
			}
		}

		public void UpdateWindowTitle()
        {
            EditorElement ee = GetCurrentEditorElement();
            string outString;
            if (ee == null)
            {
                outString = "SPEdit";
                Program.discordClient.SetPresence(new RichPresence()
                {
                    State = "Idle",
                    Timestamps = Program.discordTime,
                    Assets = new Assets()
                    {
                        LargeImageKey = "immagine",
                    }
                });
            }
            else
            {
                outString = ee.FullFilePath + " - SPEdit";
            }
            if (ServerIsRunning)
            {
                outString = $"{outString} ({Program.Translations.GetLanguage("ServerRunning")})";
            }
            Title = outString;
        }

        private int GetLineInteger(string lineStr)
        {
            int end = 0;
            for (int i = 0; i < lineStr.Length; ++i)
            {
                if (lineStr[i] >= '0' && lineStr[i] <= '9')
                {
                    end = i;
                }
                else
                {
                    break;
                }
            }

            if (int.TryParse(lineStr.Substring(0, end + 1), out var line))
            {
                return line;
            }
            return -1;
        }

        private ObservableCollection<string> compileButtonDict = new ObservableCollection<string>() { Program.Translations.GetLanguage("CompileAll"), Program.Translations.GetLanguage("CompileCurr") };
        private ObservableCollection<string> actionButtonDict = new ObservableCollection<string>() { Program.Translations.GetLanguage("Copy"), Program.Translations.GetLanguage("FTPUp"), Program.Translations.GetLanguage("StartServer") };
        private ObservableCollection<string> findReplaceButtonDict = new ObservableCollection<string>() { Program.Translations.GetLanguage("Replace"), Program.Translations.GetLanguage("ReplaceAll") };
    }
}
