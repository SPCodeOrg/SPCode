using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DiscordRPC;
using MahApps.Metro;
using SPCode.Interop.Updater;
using SPCode.UI.Components;
using Xceed.Wpf.AvalonDock;
using Xceed.Wpf.AvalonDock.Layout;

// using SPCode.Interop.Updater; //not delete! ?

namespace SPCode.UI
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly Storyboard BlendOverEffect;
        private readonly Storyboard DisableServerAnim;
        public readonly List<EditorElement> EditorsReferences = new List<EditorElement>();
        private readonly Storyboard EnableServerAnim;
        private readonly Storyboard FadeFindReplaceGridIn;
        private readonly Storyboard FadeFindReplaceGridOut;

        private readonly bool FullyInitialized;

        private ObservableCollection<string> actionButtonDict = new ObservableCollection<string>
        {
            Program.Translations.GetLanguage("Copy"), Program.Translations.GetLanguage("FTPUp"),
            Program.Translations.GetLanguage("StartServer")
        };

        private ObservableCollection<string> compileButtonDict = new ObservableCollection<string>
            {Program.Translations.GetLanguage("CompileAll"), Program.Translations.GetLanguage("CompileCurr")};

        private ObservableCollection<string> findReplaceButtonDict = new ObservableCollection<string>
            {Program.Translations.GetLanguage("Replace"), Program.Translations.GetLanguage("ReplaceAll")};

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

            ObjectBrowserDirList.ItemsSource = Program.Configs[Program.SelectedConfig].SMDirectories;
            ObjectBrowserDirList.SelectedIndex = 0;

            MetroDialogOptions.AnimateHide = MetroDialogOptions.AnimateShow = false;
            BlendOverEffect = (Storyboard)Resources["BlendOverEffect"];
            FadeFindReplaceGridIn = (Storyboard)Resources["FadeFindReplaceGridIn"];
            FadeFindReplaceGridOut = (Storyboard)Resources["FadeFindReplaceGridOut"];
            EnableServerAnim = (Storyboard)Resources["EnableServerAnim"];
            DisableServerAnim = (Storyboard)Resources["DisableServerAnim"];
            ChangeObjectBrowserToDirectory(Program.OptionsObject.Program_ObjectBrowserDirectory);
            Language_Translate(true);

            if (Program.OptionsObject.LastOpenFiles != null)
            {
                foreach (var file in Program.OptionsObject.LastOpenFiles)
                {
                    TryLoadSourceFile(file, false);
                }
            }

            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; ++i)
            {
                if (!args[i].EndsWith("exe"))
                {
                    TryLoadSourceFile(args[i], false, true, i == 0);
                }
            }

            sc.Close(TimeSpan.FromMilliseconds(500.0));
            // StartBackgroundParserThread();
            FullyInitialized = true;
        }

        public bool TryLoadSourceFile(string filePath, bool UseBlendoverEffect = true, bool TryOpenIncludes = true,
            bool SelectMe = false)
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                var extension = fileInfo.Extension.ToLowerInvariant().Trim('.', ' ');
                if (extension == "sp" || extension == "inc" || extension == "txt" || extension == "cfg" ||
                    extension == "ini")
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

                                return true;
                            }
                        }
                    }

                    AddEditorElement(finalPath, fileInfo.Name, SelectMe);
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
                                TryLoadSourceFile(fileName, false,
                                    Program.OptionsObject.Program_OpenIncludesRecursively);
                            }
                            catch (Exception)
                            {
                                // ignored
                            }
                        }
                    }
                }
                else if (extension == "smx")
                {
                    var layoutDocument = new LayoutDocument { Title = "DASM: " + fileInfo.Name };
                    var dasmElement = new DASMElement(fileInfo);
                    layoutDocument.Content = dasmElement;
                    DockingPane.Children.Add(layoutDocument);
                    DockingPane.SelectedContentIndex = DockingPane.ChildrenCount - 1;
                }

                if (UseBlendoverEffect)
                {
                    BlendOverEffect.Begin();
                }
                ChangeObjectBrowserToDirectory(fileInfo.DirectoryName);
                ObjectBrowserButtonHolder.SelectedIndex = 0;
                return true;
            }

            return false;
        }

        private void AddEditorElement(string filePath, string name, bool SelectMe)
        {
            var layoutDocument = new LayoutDocument { Title = name };
            layoutDocument.ToolTip = filePath;
            var editor = new EditorElement(filePath) { Parent = layoutDocument };
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
        }

        private void DockingManager_DocumentClosed(object sender, DocumentClosedEventArgs e)
        {
            (e.Document.Content as EditorElement)?.Close();
            UpdateWindowTitle();
        }

        // Code taken from VisualPawn Editer (Not published yet)
        private void DockingPaneGroup_ChildrenTreeChanged(object sender, ChildrenTreeChangedEventArgs e)
        {
            // if the active LayoutDocumentPane gets closed 
            // 1. it will not be in the LayoutDocumentPaneGroup.
            // 2. editor that get added to it will not be shown in the client.
            // Solution: Set the active LayoutDocumentPane to the first LayoutDocumentPaneGroup avilable child.
            if (e.Change == ChildrenTreeChange.DirectChildrenChanged
                && !DockingPaneGroup.Children.Contains(DockingPane)
                && DockingPaneGroup.Children[0] is LayoutDocumentPane pane)
            {
                DockingPane = pane;
            }
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            ServerCheckThread?.Abort(); //a join would not work, so we have to be..forcefully...
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

            Program.discordClient.Dispose();
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
                    TryLoadSourceFile(files[i], i == 0, true, i == 0);
                }
            }
        }

        private static void ProcessUITasks()
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

        private void ErrorResultGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var row = (ErrorDataGridRow)ErrorResultGrid.SelectedItem;
            if (row == null)
            {
                return;
            }

            var fileName = row.File;
            var editors = GetAllEditorElements();
            if (editors == null)
            {
                return;
            }

            foreach (var editor in editors)
            {
                if (editor.FullFilePath == fileName)
                {
                    editor.Parent.IsSelected = true;
                    var line = GetLineInteger(row.Line);
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
            var ee = GetCurrentEditorElement();
            var someEditorIsOpen = ee != null;
            var outString = "Idle";
            if (someEditorIsOpen)
            {
                outString = ee.FullFilePath;
                ee.editor.Focus();
            }

            outString += " - SPCode";

            if (Program.discordClient.IsInitialized)
            {
                Program.discordClient.SetPresence(new RichPresence
                {
                    Timestamps = Program.discordTime,
                    State = someEditorIsOpen ? $"Editing {Path.GetFileName(ee.FullFilePath)}" : "Idle",
                    Assets = new Assets
                    {
                        LargeImageKey = "immagine",
                    }
                });
            }

            if (ServerIsRunning)
            {
                outString = $"{outString} | ({Program.Translations.GetLanguage("ServerRunning")})";
            }

            Title = outString;
        }

        private int GetLineInteger(string lineStr)
        {
            var end = 0;
            for (var i = 0; i < lineStr.Length; ++i)
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
    }
}