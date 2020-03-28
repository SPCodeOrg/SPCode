using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;
using MahApps.Metro.Controls.Dialogs;
using Spedit.Utils.SPSyntaxTidy;
using Xceed.Wpf.AvalonDock.Layout;
using Timer = System.Timers.Timer;

namespace Spedit.UI.Components
{
	/// <summary>
	///     Interaction logic for EditorElement.xaml
	/// </summary>
	public partial class EditorElement : UserControl
    {
        private string _FullFilePath = "";

        private bool _NeedsSave;
        public Timer AutoSaveTimer;
        private readonly BracketHighlightRenderer bracketHighlightRenderer;
        private readonly SPBracketSearcher bracketSearcher;
        private readonly ColorizeSelection colorizeSelection;

        private readonly Storyboard FadeJumpGridIn;
        private readonly Storyboard FadeJumpGridOut;

        private FileSystemWatcher fileWatcher;

        public FoldingManager foldingManager;
        private readonly SPFoldingStrategy foldingStrategy;

        private bool isBlock;

        private bool JumpGridIsOpen;

        private double LineHeight;
        public new LayoutDocument Parent;

        private readonly Timer regularyTimer;
        private bool SelectionIsHighlited;
        private bool WantFoldingUpdate;

        public EditorElement()
        {
            InitializeComponent();
        }

        public EditorElement(string filePath)
        {
            InitializeComponent();

            bracketSearcher = new SPBracketSearcher();
            bracketHighlightRenderer = new BracketHighlightRenderer(editor.TextArea.TextView);
            editor.TextArea.IndentationStrategy = new EditorIndetationStrategy();

            FadeJumpGridIn = (Storyboard) Resources["FadeJumpGridIn"];
            FadeJumpGridOut = (Storyboard) Resources["FadeJumpGridOut"];

            KeyDown += EditorElement_KeyDown;

            editor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
            editor.TextArea.SelectionChanged += TextArea_SelectionChanged;
            editor.TextArea.PreviewKeyDown += TextArea_PreviewKeyDown;
            editor.PreviewMouseWheel += PrevMouseWheel;
            editor.MouseDown += editor_MouseDown;
            editor.TextArea.TextEntered += TextArea_TextEntered;
            editor.TextArea.TextEntering += TextArea_TextEntering;

            var fInfo = new FileInfo(filePath);
            if (fInfo.Exists)
            {
                fileWatcher = new FileSystemWatcher(fInfo.DirectoryName) {IncludeSubdirectories = false};
                fileWatcher.NotifyFilter = NotifyFilters.Size | NotifyFilters.LastWrite;
                fileWatcher.Filter = "*" + fInfo.Extension;
                fileWatcher.Changed += fileWatcher_Changed;
                fileWatcher.EnableRaisingEvents = true;
            }
            else
            {
                fileWatcher = null;
            }

            _FullFilePath = filePath;
            editor.Options.ConvertTabsToSpaces = false;
            editor.Options.EnableHyperlinks = false;
            editor.Options.EnableEmailHyperlinks = false;
            editor.Options.HighlightCurrentLine = true;
            editor.Options.AllowScrollBelowDocument = true;
            editor.Options.ShowSpaces = Program.OptionsObject.Editor_ShowSpaces;
            editor.Options.ShowTabs = Program.OptionsObject.Editor_ShowTabs;
            editor.Options.IndentationSize = Program.OptionsObject.Editor_IndentationSize;
            editor.TextArea.SelectionCornerRadius = 0.0;
            editor.Options.ConvertTabsToSpaces = Program.OptionsObject.Editor_ReplaceTabsToWhitespace;

            Brush currentLineBackground = new SolidColorBrush(Color.FromArgb(0x20, 0x88, 0x88, 0x88));
            Brush currentLinePenBrush = new SolidColorBrush(Color.FromArgb(0x30, 0x88, 0x88, 0x88));
            currentLinePenBrush.Freeze();
            var currentLinePen = new Pen(currentLinePenBrush, 1.0);
            currentLineBackground.Freeze();
            currentLinePen.Freeze();
            editor.TextArea.TextView.CurrentLineBackground = currentLineBackground;
            editor.TextArea.TextView.CurrentLineBorder = currentLinePen;

            editor.FontFamily = new FontFamily(Program.OptionsObject.Editor_FontFamily);
            editor.WordWrap = Program.OptionsObject.Editor_WordWrap;
            UpdateFontSize(Program.OptionsObject.Editor_FontSize, false);

            colorizeSelection = new ColorizeSelection();
            editor.TextArea.TextView.LineTransformers.Add(colorizeSelection);
            editor.SyntaxHighlighting = new AeonEditorHighlighting();

            LoadAutoCompletes();

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = FileReader.OpenStream(fs, Encoding.UTF8))
                {
                    var source = reader.ReadToEnd();
                    source = source.Replace("\r\n", "\n").Replace("\r", "\n")
                        .Replace("\n", "\r\n"); //normalize line endings
                    editor.Text = source;
                }
            }

            _NeedsSave = false;

            Language_Translate(true); //The Fontsize and content must be loaded

            var encoding = new UTF8Encoding(false);
            editor.Encoding = encoding; //let them read in whatever encoding they want - but save in UTF8

            foldingManager = FoldingManager.Install(editor.TextArea);
            foldingStrategy = new SPFoldingStrategy();
            foldingStrategy.UpdateFoldings(foldingManager, editor.Document);

            regularyTimer = new Timer(500.0);
            regularyTimer.Elapsed += regularyTimer_Elapsed;
            regularyTimer.Start();

            AutoSaveTimer = new Timer();
            AutoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;
            StartAutoSaveTimer();

            CompileBox.IsChecked = filePath.EndsWith(".sp");
        }

        public string FullFilePath
        {
            get => _FullFilePath;
            set
            {
                var fInfo = new FileInfo(value);
                _FullFilePath = fInfo.FullName;
                Parent.Title = fInfo.Name;
                if (fileWatcher != null) fileWatcher.Path = fInfo.DirectoryName;
            }
        }

        public bool NeedsSave
        {
            get => _NeedsSave;
            set
            {
                if (!(value ^ _NeedsSave)) //when not changed
                    return;
                _NeedsSave = value;
                if (Parent != null)
                {
                    if (_NeedsSave)
                        Parent.Title = "*" + Parent.Title;
                    else
                        Parent.Title = Parent.Title.Trim('*');
                }
            }
        }

        private void AutoSaveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (NeedsSave)
                Dispatcher.Invoke(() => { Save(); });
        }

        public void StartAutoSaveTimer()
        {
            if (Program.OptionsObject.Editor_AutoSave)
            {
                if (AutoSaveTimer.Enabled) AutoSaveTimer.Stop();
                AutoSaveTimer.Interval = 1000.0 * Program.OptionsObject.Editor_AutoSaveInterval;
                AutoSaveTimer.Start();
            }
        }

        private void EditorElement_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.G)
                if (Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightAlt))
                {
                    ToggleJumpGrid();
                    e.Handled = true;
                }
        }

        public void ToggleJumpGrid()
        {
            if (JumpGridIsOpen)
            {
                FadeJumpGridOut.Begin();
                JumpGridIsOpen = false;
            }
            else
            {
                FadeJumpGridIn.Begin();
                JumpGridIsOpen = true;
                JumpNumber.Focus();
                JumpNumber.SelectAll();
            }
        }

        private void JumpNumberKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                JumpToNumber(null, null);
                e.Handled = true;
            }
        }

        private void JumpToNumber(object sender, RoutedEventArgs e)
        {
            int num;
            if (int.TryParse(JumpNumber.Text, out num))
            {
                if (LineJump.IsChecked.Value)
                {
                    num = Math.Max(1, Math.Min(num, editor.LineCount));
                    var line = editor.Document.GetLineByNumber(num);
                    if (line != null)
                    {
                        editor.ScrollToLine(num);
                        editor.Select(line.Offset, line.Length);
                        editor.CaretOffset = line.Offset;
                    }
                }
                else
                {
                    num = Math.Max(0, Math.Min(num, editor.Text.Length));
                    var line = editor.Document.GetLineByOffset(num);
                    if (line != null)
                    {
                        editor.ScrollTo(line.LineNumber, 0);
                        editor.CaretOffset = num;
                    }
                }
            }

            ToggleJumpGrid();
            editor.Focus();
        }

        private void fileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e == null) return;
            if (e.FullPath == _FullFilePath)
            {
                var ReloadFile = false;
                if (_NeedsSave)
                {
                    var result = MessageBox.Show(
                        string.Format(Program.Translations.GetLanguage("DFileChanged"), _FullFilePath) +
                        Environment.NewLine + Program.Translations.GetLanguage("FileTryReload"),
                        Program.Translations.GetLanguage("FileChanged"), MessageBoxButton.YesNo,
                        MessageBoxImage.Asterisk);
                    ReloadFile = result == MessageBoxResult.Yes;
                }
                else //when the user didnt changed anything, we just reload the file since we are intelligent...
                {
                    ReloadFile = true;
                }

                if (ReloadFile)
                    Dispatcher.Invoke(() =>
                    {
                        FileStream stream;
                        var IsNotAccessed = true;
                        while (IsNotAccessed)
                        {
                            try
                            {
                                using (stream = new FileStream(_FullFilePath, FileMode.OpenOrCreate))
                                {
                                    editor.Load(stream);
                                    NeedsSave = false;
                                    IsNotAccessed = false;
                                }
                            }
                            catch (Exception)
                            {
                            }

                            Thread.Sleep(
                                100); //dont include System.Threading in the using directives, cause its onlyused once and the Timer class will double
                        }
                    });
            }
        }

        private void regularyTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (editor.SelectionLength > 0 && editor.SelectionLength < 50)
                {
                    var selectionString = editor.SelectedText;
                    if (IsValidSearchSelectionString(selectionString))
                    {
                        colorizeSelection.SelectionString = selectionString;
                        colorizeSelection.HighlightSelection = true;
                        SelectionIsHighlited = true;
                        editor.TextArea.TextView.Redraw();
                    }
                    else
                    {
                        colorizeSelection.HighlightSelection = false;
                        colorizeSelection.SelectionString = string.Empty;
                        if (SelectionIsHighlited)
                        {
                            editor.TextArea.TextView.Redraw();
                            SelectionIsHighlited = false;
                        }
                    }
                }
                else
                {
                    colorizeSelection.HighlightSelection = false;
                    colorizeSelection.SelectionString = string.Empty;
                    if (SelectionIsHighlited)
                    {
                        editor.TextArea.TextView.Redraw();
                        SelectionIsHighlited = false;
                    }
                }
            });
            if (WantFoldingUpdate)
            {
                WantFoldingUpdate = false;
                try //this "solves" a racing-conditions error - i wasnt able to fix it till today.. 
                {
                    Dispatcher.Invoke(() => { foldingStrategy.UpdateFoldings(foldingManager, editor.Document); });
                }
                catch (Exception)
                {
                }
            }
        }

        public void Save(bool Force = false)
        {
            if (_NeedsSave || Force)
            {
                if (fileWatcher != null) fileWatcher.EnableRaisingEvents = false;
                try
                {
                    using (var fs = new FileStream(_FullFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        editor.Save(fs);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(Program.MainWindow,
                        Program.Translations.GetLanguage("DSaveError") + Environment.NewLine + "(" + e.Message + ")",
                        Program.Translations.GetLanguage("SaveError"),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                NeedsSave = false;
                if (fileWatcher != null) fileWatcher.EnableRaisingEvents = true;
            }
        }

        public void UpdateFontSize(double size, bool UpdateLineHeight = true)
        {
            if (size > 2 && size < 31)
            {
                editor.FontSize = size;
                StatusLine_FontSize.Text = size.ToString("n0") + $" {Program.Translations.GetLanguage("PtAbb")}";
            }

            if (UpdateLineHeight) LineHeight = editor.TextArea.TextView.DefaultLineHeight;
        }

        public void ToggleCommentOnLine()
        {
            var line = editor.Document.GetLineByOffset(editor.CaretOffset);
            var lineText = editor.Document.GetText(line.Offset, line.Length);
            var leadinggWhiteSpaces = 0;
            for (var i = 0; i < lineText.Length; ++i)
                if (char.IsWhiteSpace(lineText[i]))
                    leadinggWhiteSpaces++;
                else
                    break;
            lineText = lineText.Trim();
            if (lineText.Length > 1)
            {
                if (lineText[0] == '/' && lineText[1] == '/')
                    editor.Document.Remove(line.Offset + leadinggWhiteSpaces, 2);
                else
                    editor.Document.Insert(line.Offset + leadinggWhiteSpaces, "//");
            }
            else
            {
                editor.Document.Insert(line.Offset + leadinggWhiteSpaces, "//");
            }
        }

        public void DuplicateLine(bool down)
        {
            var line = editor.Document.GetLineByOffset(editor.CaretOffset);
            var lineText = editor.Document.GetText(line.Offset, line.Length);
            editor.Document.Insert(line.Offset, lineText + Environment.NewLine);
            if (down) editor.CaretOffset -= line.Length + 1;
        }

        public void MoveLine(bool down)
        {
            var line = editor.Document.GetLineByOffset(editor.CaretOffset);
            if (down)
            {
                if (line.NextLine == null)
                {
                    editor.Document.Insert(line.Offset, Environment.NewLine);
                }
                else
                {
                    var lineText = editor.Document.GetText(line.NextLine.Offset, line.NextLine.Length);
                    editor.Document.Remove(line.NextLine.Offset, line.NextLine.TotalLength);
                    editor.Document.Insert(line.Offset, lineText + Environment.NewLine);
                }
            }
            else
            {
                if (line.PreviousLine == null)
                {
                    editor.Document.Insert(line.Offset + line.Length, Environment.NewLine);
                }
                else
                {
                    var insertOffset = line.PreviousLine.Offset;
                    var relativeCaretOffset = editor.CaretOffset - line.Offset;
                    var lineText = editor.Document.GetText(line.Offset, line.Length);
                    editor.Document.Remove(line.Offset, line.TotalLength);
                    editor.Document.Insert(insertOffset, lineText + Environment.NewLine);
                    editor.CaretOffset = insertOffset + relativeCaretOffset;
                }
            }
        }

        public async void Close(bool ForcedToSave = false, bool CheckSavings = true)
        {
            regularyTimer.Stop();
            regularyTimer.Close();
            if (fileWatcher != null)
            {
                fileWatcher.EnableRaisingEvents = false;
                fileWatcher.Dispose();
                fileWatcher = null;
            }

            if (CheckSavings)
                if (_NeedsSave)
                {
                    if (ForcedToSave)
                    {
                        Save();
                    }
                    else
                    {
                        var title = $"{Program.Translations.GetLanguage("SavingFile")} '" + Parent.Title.Trim('*') +
                                    "'";
                        var msg = "";
                        var Result = await Program.MainWindow.ShowMessageAsync(title, msg,
                            MessageDialogStyle.AffirmativeAndNegative, Program.MainWindow.MetroDialogOptions);
                        if (Result == MessageDialogResult.Affirmative) Save();
                    }
                }

            Program.MainWindow.EditorsReferences.Remove(this);
            var childs = Program.MainWindow.DockingPaneGroup.Children;
            foreach (var c in childs)
                if (c is LayoutDocumentPane)
                    ((LayoutDocumentPane) c).Children.Remove(Parent);
            Parent = null; //to prevent a ring depency which disables the GC from work
            Program.MainWindow.UpdateWindowTitle();
        }

        private void editor_TextChanged(object sender, EventArgs e)
        {
            WantFoldingUpdate = true;
            NeedsSave = true;
        }

        private void Caret_PositionChanged(object sender, EventArgs e)
        {
            StatusLine_Coloumn.Text = $"{Program.Translations.GetLanguage("ColAbb")} {editor.TextArea.Caret.Column}";
            StatusLine_Line.Text = $"{Program.Translations.GetLanguage("LnAbb")} {editor.TextArea.Caret.Line}";
            EvaluateIntelliSense();
            var result = bracketSearcher.SearchBracket(editor.Document, editor.CaretOffset);
            bracketHighlightRenderer.SetHighlight(result);
        }

        private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (Program.OptionsObject.Editor_ReformatLineAfterSemicolon)
                if (e.Text == ";")
                    if (editor.CaretOffset >= 0)
                    {
                        var line = editor.Document.GetLineByOffset(editor.CaretOffset);
                        var leadingIndentation =
                            editor.Document.GetText(TextUtilities.GetLeadingWhitespace(editor.Document, line));
                        var newLineStr = leadingIndentation + SPSyntaxTidy.TidyUp(editor.Document.GetText(line)).Trim();
                        editor.Document.Replace(line, newLineStr);
                    }

            switch (e.Text)
            {
                case "\n":
                    if (!isBlock)
                        break;

                    editor.TextArea.Caret.Line -= 1;
                    editor.TextArea.Caret.Column += 1;
                    isBlock = false;
                    break;
                case "}":
                    // Seems like this is not required
                    // editor.TextArea.IndentationStrategy.IndentLine(editor.Document, editor.Document.GetLineByOffset(editor.CaretOffset));
                    foldingStrategy.UpdateFoldings(foldingManager, editor.Document);
                    break;
                case "{":
                {
                    if (Program.OptionsObject.Editor_AutoCloseBrackets)
                    {
                        editor.Document.Insert(editor.CaretOffset, "}");
                        editor.CaretOffset -= 1;
                    }

                    foldingStrategy.UpdateFoldings(foldingManager, editor.Document);
                    break;
                }
                default:
                {
                    if (Program.OptionsObject.Editor_AutoCloseBrackets)
                    {
                        if (e.Text == "(")
                        {
                            editor.Document.Insert(editor.CaretOffset, ")");
                            editor.CaretOffset -= 1;
                        }
                        else if (e.Text == "[")
                        {
                            editor.Document.Insert(editor.CaretOffset, "]");
                            editor.CaretOffset -= 1;
                        }
                    }

                    break;
                }
            }

            if (Program.OptionsObject.Editor_AutoCloseStringChars)
            {
                if (e.Text == "\"")
                {
                    var line = editor.Document.GetLineByOffset(editor.CaretOffset);
                    var lineText = editor.Document.GetText(line.Offset, editor.CaretOffset - line.Offset);
                    if (lineText.Length > 0)
                        if (lineText[Math.Max(lineText.Length - 2, 0)] != '\\')
                        {
                            editor.Document.Insert(editor.CaretOffset, "\"");
                            editor.CaretOffset -= 1;
                        }
                }
                else if (e.Text == "'")
                {
                    var line = editor.Document.GetLineByOffset(editor.CaretOffset);
                    var lineText = editor.Document.GetText(line.Offset, editor.CaretOffset - line.Offset);
                    if (lineText.Length > 0)
                        if (lineText[Math.Max(lineText.Length - 2, 0)] != '\\')
                        {
                            editor.Document.Insert(editor.CaretOffset, "'");
                            editor.CaretOffset -= 1;
                        }
                }
            }
        }

        private void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == "\n")
            {
                if (editor.Document.TextLength < editor.CaretOffset+1)
                    return;
                
                var segment = new AnchorSegment(editor.Document, editor.CaretOffset - 1, 2);
                var text = editor.Document.GetText(segment);
                if (text == "{}")
                    isBlock = true;
            }
        }

        private void TextArea_SelectionChanged(object sender, EventArgs e)
        {
            StatusLine_SelectionLength.Text = $"{Program.Translations.GetLanguage("LenAbb")} {editor.SelectionLength}";
        }

        private void PrevMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                UpdateFontSize(editor.FontSize + Math.Sign(e.Delta));
                e.Handled = true;
            }
            else
            {
                if (LineHeight == 0.0) LineHeight = editor.TextArea.TextView.DefaultLineHeight;
                editor.ScrollToVerticalOffset(editor.VerticalOffset -
                                              Math.Sign((double) e.Delta) * LineHeight *
                                              Program.OptionsObject.Editor_ScrollLines);
                //editor.ScrollToVerticalOffset(editor.VerticalOffset - ((double)e.Delta * editor.FontSize * Program.OptionsObject.Editor_ScrollSpeed));
                e.Handled = true;
            }

            HideISAC();
        }

        private void editor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HideISAC();
        }

        private void TextArea_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = ISAC_EvaluateKeyDownEvent(e.Key);
            if (!e.Handled
            ) //one could ask why some key-bindings are handled here. Its because spedit sends handled flags for ups&downs and they are therefore not able to processed by the central code.
                if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl))
                {
                    if (e.KeyboardDevice.IsKeyDown(Key.LeftAlt))
                    {
                        if (e.Key == Key.Down)
                        {
                            DuplicateLine(true);
                            e.Handled = true;
                        }
                        else if (e.Key == Key.Up)
                        {
                            DuplicateLine(false);
                            e.Handled = true;
                        }
                    }
                    else
                    {
                        if (e.Key == Key.Down)
                        {
                            MoveLine(true);
                            e.Handled = true;
                        }
                        else if (e.Key == Key.Up)
                        {
                            MoveLine(false);
                            e.Handled = true;
                        }
                    }
                }
        }

        private void HandleContextMenuCommand(object sender, RoutedEventArgs e)
        {
            switch ((string) ((MenuItem) sender).Tag)
            {
                case "0":
                {
                    editor.Undo();
                    break;
                }
                case "1":
                {
                    editor.Redo();
                    break;
                }
                case "2":
                {
                    editor.Cut();
                    break;
                }
                case "3":
                {
                    editor.Copy();
                    break;
                }
                case "4":
                {
                    editor.Paste();
                    break;
                }
                case "5":
                {
                    editor.SelectAll();
                    break;
                }
            }
        }

        private void ContextMenu_Opening(object sender, RoutedEventArgs e)
        {
            ((MenuItem) ((ContextMenu) sender).Items[0]).IsEnabled = editor.CanUndo;
            ((MenuItem) ((ContextMenu) sender).Items[1]).IsEnabled = editor.CanRedo;
        }

        private bool IsValidSearchSelectionString(string s)
        {
            var length = s.Length;
            for (var i = 0; i < length; ++i)
                if (!(s[i] >= 'a' && s[i] <= 'z' || s[i] >= 'A' && s[i] <= 'Z' || s[i] >= '0' && s[i] <= '9' ||
                      s[i] == '_'))
                    return false;
            return true;
        }

        public void Language_Translate(bool Initial = false)
        {
            if (Program.Translations.IsDefault) return;
            MenuC_Undo.Header = Program.Translations.GetLanguage("Undo");

            MenuC_Redo.Header = Program.Translations.GetLanguage("Redo");

            MenuC_Cut.Header = Program.Translations.GetLanguage("Cut");

            MenuC_Copy.Header = Program.Translations.GetLanguage("Copy");

            MenuC_Paste.Header = Program.Translations.GetLanguage("Paste");

            MenuC_SelectAll.Header = Program.Translations.GetLanguage("SelectAll");
            CompileBox.Content = Program.Translations.GetLanguage("Compile");
            if (!Initial)
            {
                StatusLine_Coloumn.Text =
                    $"{Program.Translations.GetLanguage("ColAbb")} {editor.TextArea.Caret.Column}";
                StatusLine_Line.Text = $"{Program.Translations.GetLanguage("LnAbb")} {editor.TextArea.Caret.Line}";
                StatusLine_FontSize.Text =
                    editor.FontSize.ToString("n0") + $" {Program.Translations.GetLanguage("PtAbb")}";
            }
        }
    }

    public class ColorizeSelection : DocumentColorizingTransformer
    {
        public bool HighlightSelection;
        public string SelectionString = string.Empty;

        protected override void ColorizeLine(DocumentLine line)
        {
            if (HighlightSelection)
            {
                if (string.IsNullOrWhiteSpace(SelectionString)) return;
                var lineStartOffset = line.Offset;
                var text = CurrentContext.Document.GetText(line);
                var start = 0;
                int index;
                while ((index = text.IndexOf(SelectionString, start)) >= 0)
                {
                    ChangeLinePart(
                        lineStartOffset + index,
                        lineStartOffset + index + SelectionString.Length,
                        element => { element.BackgroundBrush = Brushes.LightGray; });
                    start = index + 1;
                }
            }
        }
    }
}