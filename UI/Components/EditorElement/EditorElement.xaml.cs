using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;
using MahApps.Metro.Controls.Dialogs;
using SourcepawnCondenser;
using SourcepawnCondenser.SourcemodDefinition;
using SPCode.Utils.SPSyntaxTidy;
using Xceed.Wpf.AvalonDock.Layout;
using Timer = System.Timers.Timer;
using SPCode.Utils;

namespace SPCode.UI.Components
{
    /// <summary>
    ///     Interaction logic for EditorElement.xaml
    /// </summary>
    public partial class EditorElement : UserControl
    {
        #region Variables and Properties
        private readonly BracketHighlightRenderer bracketHighlightRenderer;
        private readonly SPBracketSearcher bracketSearcher;
        private readonly ColorizeSelection colorizeSelection;
        private readonly SPFoldingStrategy foldingStrategy;
        private readonly Timer regularyTimer;
        private string _FullFilePath = "";
        private bool _NeedsSave;
        public Timer AutoSaveTimer;
        private Timer parseTimer;
        private FileSystemWatcher fileWatcher;
        public FoldingManager foldingManager;
        private bool isBlock;
        private double LineHeight;
        public new LayoutDocument Parent;
        private bool SelectionIsHighlited;
        private bool WantFoldingUpdate;
        public BracketHighlightHelpers bracketHelper = new();

        public string FullFilePath
        {
            get => _FullFilePath;
            set
            {
                var fInfo = new FileInfo(value);
                _FullFilePath = fInfo.FullName;
                Parent.Title = fInfo.Name;
                if (fileWatcher != null)
                {
                    fileWatcher.Path = fInfo.DirectoryName;
                }
            }
        }
        public bool NeedsSave
        {
            get => _NeedsSave;
            set
            {
                if (!(value ^ _NeedsSave)) //when not changed
                {
                    return;
                }

                _NeedsSave = value;
                if (Parent != null)
                {
                    if (_NeedsSave)
                    {
                        Parent.Title = "*" + Parent.Title;
                    }
                    else
                    {
                        Parent.Title = Parent.Title.Trim('*');
                    }
                }
            }
        }
        #endregion

        #region Constructors
        public EditorElement()
        {
            InitializeComponent();
        }

        public EditorElement(string filePath)
        {
            InitializeComponent();

            bracketSearcher = new SPBracketSearcher();
            bracketHighlightRenderer = new BracketHighlightRenderer(editor.TextArea.TextView);
            editor.TextArea.IndentationStrategy = new EditorIndentationStrategy();

            editor.CaptureMouse();

            //KeyDown += EditorElement_KeyDown;

            editor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
            editor.TextArea.SelectionChanged += TextArea_SelectionChanged;
            editor.TextArea.PreviewKeyDown += TextArea_PreviewKeyDown;

            editor.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(TextArea_MouseDown), true);

            editor.PreviewMouseWheel += PrevMouseWheel;
            editor.MouseDown += Editor_MouseDown;
            editor.Loaded += Editor_Loaded;

            editor.TextArea.TextEntered += TextArea_TextEntered;
            editor.TextArea.TextEntering += TextArea_TextEntering;
            var fInfo = new FileInfo(filePath);
            if (fInfo.Exists)
            {
                fileWatcher = new FileSystemWatcher(fInfo.DirectoryName ?? throw new NullReferenceException())
                {
                    IncludeSubdirectories = false,
                    NotifyFilter = NotifyFilters.Size | NotifyFilters.LastWrite,
                    Filter = "*" + fInfo.Extension
                };
                fileWatcher.Changed += FileWatcher_Changed;
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

            LoadAutoCompletes();

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using var reader = FileReader.OpenStream(fs, Encoding.UTF8);
                var source = reader.ReadToEnd();
                source = source.Replace("\r\n", "\n").Replace("\r", "\n")
                    .Replace("\n", "\r\n"); //normalize line endings
                editor.Text = source;
            }

            _NeedsSave = false;

            Language_Translate(true); //The Fontsize and content must be loaded

            var encoding = new UTF8Encoding(false);
            editor.Encoding = encoding; //let them read in whatever encoding they want - but save in UTF8

            foldingManager = FoldingManager.Install(editor.TextArea);
            foldingStrategy = new SPFoldingStrategy();
            foldingStrategy.UpdateFoldings(foldingManager, editor.Document);

            regularyTimer = new Timer(500.0);
            regularyTimer.Elapsed += RegularyTimer_Elapsed;
            regularyTimer.Start();

            AutoSaveTimer = new Timer();
            AutoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;
            StartAutoSaveTimer();

            CompileBox.IsChecked = filePath.EndsWith(".sp");
        }
        #endregion

        #region General events
        private void Editor_Loaded(object sender, RoutedEventArgs e)
        {
            ParseIncludes(sender, e);
        }

        private async void TextArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                return;
            }
            
            await GoToDefinition(e);
        }

        private void AutoSaveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (NeedsSave)
            {
                Dispatcher.Invoke(() => { Save(); });
            }
        }

        private void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e == null)
            {
                return;
            }

            if (e.FullPath == _FullFilePath)
            {
                bool reloadFile;
                if (_NeedsSave)
                {
                    var result = MessageBox.Show(
                        string.Format(Program.Translations.GetLanguage("DFileChanged"), _FullFilePath) +
                        Environment.NewLine + Program.Translations.GetLanguage("FileTryReload"),
                        Program.Translations.GetLanguage("FileChanged"), MessageBoxButton.YesNo,
                        MessageBoxImage.Asterisk);
                    reloadFile = result == MessageBoxResult.Yes;
                }
                else //when the user didnt changed anything, we just reload the file since we are intelligent...
                {
                    reloadFile = true;
                }

                if (reloadFile)
                {
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
                                // ignored
                            }

                            Thread.Sleep(
                                100); //dont include System.Threading in the using directives, cause its onlyused once and the Timer class will double
                        }
                    });
                }
            }
        }

        private void RegularyTimer_Elapsed(object sender, ElapsedEventArgs e)
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
                    // ignored
                }
            }
        }

        private void Editor_TextChanged(object sender, EventArgs e)
        {
            WantFoldingUpdate = true;
            NeedsSave = true;
        }

        private void Caret_PositionChanged(object sender, EventArgs e)
        {
            StatusLine_Column.Text = $"{Program.Translations.GetLanguage("ColAbb")} {editor.TextArea.Caret.Column}";
            StatusLine_Line.Text = $"{Program.Translations.GetLanguage("LnAbb")} {editor.TextArea.Caret.Line}";
#if DEBUG
            StatusLine_Offset.Text = $"Off {editor.TextArea.Caret.Offset}";
#endif
            EvaluateIntelliSense();

            var result = bracketSearcher.SearchBracket(editor.Document, editor.CaretOffset);
            bracketHighlightRenderer.SetHighlight(result);

            if (!Program.OptionsObject.Program_DynamicISAC || Program.MainWindow == null)
            {
                return;
            }

            if (parseTimer != null)
            {
                parseTimer.Enabled = false;
                parseTimer.Close();
            }

            parseTimer = new Timer(200)
            {
                AutoReset = false,
                Enabled = true,
            };
            parseTimer.Elapsed += ParseIncludes;
        }

        public void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (Program.OptionsObject.Editor_ReformatLineAfterSemicolon)
            {
                if (e.Text == ";" && !bracketHelper.CheckForString(editor.Document, editor.CaretOffset - 1))
                {
                    if (editor.CaretOffset >= 0)
                    {
                        var line = editor.Document.GetLineByOffset(editor.CaretOffset);
                        var text = editor.Document.GetText(line);

                        // TODO: Poor way to fix this but atm I have no idea on how to fix this properly
                        if (!text.Contains("for"))
                        {
                            var leadingIndentation =
                                editor.Document.GetText(TextUtilities.GetLeadingWhitespace(editor.Document, line));
                            var newLineStr = leadingIndentation +
                                             SPSyntaxTidy.TidyUp(text).Trim();
                            editor.Document.Replace(line, newLineStr);
                        }
                    }
                }
            }

            switch (e.Text)
            {
                case "\n":
                    if (!isBlock)
                    {
                        break;
                    }

                    editor.TextArea.Caret.Line -= 1;
                    editor.TextArea.Caret.Column += Program.Indentation.Length;
                    isBlock = false;
                    break;
                case "}":
                    // Seems like this is not required
                    // editor.TextArea.IndentationStrategy.IndentLine(editor.Document, editor.Document.GetLineByOffset(editor.CaretOffset));
                    foldingStrategy.UpdateFoldings(foldingManager, editor.Document);
                    break;
                case "(":
                case "[":
                case "{":
                    if (Program.OptionsObject.Editor_AutoCloseBrackets)
                    {
                        var document = editor.Document;
                        var offset = editor.TextArea.Caret.Offset;
                        if (editor.SelectionLength == 0 && offset + 1 < document.TextLength &&
                            (bracketHelper.CheckForCommentBlockForward(document, offset) ||
                            bracketHelper.CheckForCommentBlockBackward(document, offset) ||
                            bracketHelper.CheckForCommentLine(document, offset) ||
                            bracketHelper.CheckForString(document, offset) ||
                            bracketHelper.CheckForChar(document, offset)))
                        {
                            break;
                        }

                        // Getting the char ascii code with int cast and the string pos 0 (the char it self),
                        // if it's a ( i need to add 1 to get the ascii code for closing bracket
                        // for [ and { i need to add 2 to get the closing bracket ascii code
                        var closingBracket = (char)(e.Text[0] + (e.Text == "(" ? 1 : 2));
                        editor.Document.Insert(editor.CaretOffset, closingBracket.ToString());
                        if (editor.SelectionLength == 0)
                        {
                            editor.CaretOffset -= 1;
                        }

                        // If it's a code block bracket we need to update the folding
                        if (e.Text == "{")
                        {
                            foldingStrategy.UpdateFoldings(foldingManager, editor.Document);
                        }
                    }

                    break;
                case "\"":
                case "'":
                    if (Program.OptionsObject.Editor_AutoCloseStringChars)
                    {
                        var line = editor.Document.GetLineByOffset(editor.CaretOffset);
                        var lineText = editor.Document.GetText(line.Offset, editor.CaretOffset - line.Offset);
                        if (editor.SelectionLength > 0 || (lineText.Length > 0 && lineText[Math.Max(lineText.Length - 2, 0)] != '\\'))
                        {
                            editor.Document.Insert(editor.CaretOffset, e.Text);
                            if (editor.SelectionLength == 0)
                            {
                                editor.CaretOffset -= 1;
                            }
                        }
                    }
                    break;
            }
        }

        private void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            switch (e.Text)
            {
                default:
                    return;

                case "\n":
                    if (editor.Document.TextLength < editor.CaretOffset + 1 || editor.CaretOffset < 3)
                    {
                        return;
                    }

                    var segment = new AnchorSegment(editor.Document, editor.CaretOffset - 1, 2);
                    var text = editor.Document.GetText(segment);
                    if (text == "{}")
                    {
                        isBlock = true;
                    }

                    return;

                case "(":
                case "[":
                case "{":
                    if (!Program.OptionsObject.Editor_AutoCloseBrackets)
                    {
                        return;
                    }

                    break;

                case "\"":
                case "'":
                    if (!Program.OptionsObject.Editor_AutoCloseStringChars)
                    {
                        return;
                    }

                    break;
            }

            var selectionLength = editor.SelectionLength;
            if (selectionLength > 0)
            {
                editor.Document.BeginUpdate();
                editor.Document.Insert(editor.SelectionStart, e.Text);
                editor.CaretOffset = editor.SelectionStart + editor.SelectionLength;
                TextArea_TextEntered(sender, e);
                e.Handled = true;
                editor.Document.EndUpdate();
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
                if (LineHeight == 0.0)
                {
                    LineHeight = editor.TextArea.TextView.DefaultLineHeight;
                }

                editor.ScrollToVerticalOffset(editor.VerticalOffset -
                                              (Math.Sign((double)e.Delta) * LineHeight *
                                              Program.OptionsObject.Editor_ScrollLines));
                //editor.ScrollToVerticalOffset(editor.VerticalOffset - ((double)e.Delta * editor.FontSize * Program.OptionsObject.Editor_ScrollSpeed));
                e.Handled = true;
            }

            HideISAC();
        }

        private void Editor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            HideISAC();
        }

        private void TextArea_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = ISAC_EvaluateKeyDownEvent(e.Key);
            if (!e.IsDown || e.Handled)
            {
                return;
            }

            var key = e.Key;
            var modifiers = Keyboard.Modifiers;

            if (key == Key.System)
            {
                key = e.SystemKey;
            }

            if (key == Key.LeftCtrl ||
                key == Key.RightCtrl ||
                key == Key.LeftAlt ||
                key == Key.RightAlt ||
                key == Key.LeftShift ||
                key == Key.RightShift ||
                key == Key.LWin ||
                key == Key.RWin ||
                key == Key.Clear ||
                key == Key.OemClear ||
                key == Key.Apps)
            {
                return;
            }

            Program.MainWindow.ProcessHotkey(new Hotkey(key, modifiers), e);
        }

        private void HandleContextMenuCommand(object sender, RoutedEventArgs e)
        {
            switch ((string)((MenuItem)sender).Tag)
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
            ((MenuItem)((ContextMenu)sender).Items[0]).IsEnabled = editor.CanUndo;
            ((MenuItem)((ContextMenu)sender).Items[1]).IsEnabled = editor.CanRedo;
        }
        #endregion

        #region General methods
        private void ParseIncludes(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (Program.MainWindow == null)
                {
                    return;
                }

                var ee = Program.MainWindow.GetAllEditorElements();
                var ce = Program.MainWindow.GetCurrentEditorElement();

                var caret = -1;

                if (ee == null || ce == null)
                {
                    return;
                }

                var definitions = new SMDefinition[ee.Length];
                List<SMFunction> currentFunctions = null;
                for (var i = 0; i < ee.Length; ++i)
                {
                    var el = ee[i];
                    var fInfo = new FileInfo(el.FullFilePath);
                    var text = el.editor.Document.Text;
                    if (fInfo.Extension.Trim('.').ToLowerInvariant() == "inc")
                    {
                        definitions[i] =
                            new Condenser(text
                                , fInfo.Name).Condense();
                    }

                    if (fInfo.Extension.Trim('.').ToLowerInvariant() == "sp")
                    {
                        if (el.IsLoaded)
                        {
                            caret = el.editor.CaretOffset;
                            definitions[i] =
                                new Condenser(text, fInfo.Name)
                                    .Condense();
                            currentFunctions = definitions[i].Functions;
                            if (el == ce)
                            {
                                currentSmDef = definitions[i];
                                var caret1 = caret;
                                currentSmDef.currentFunction =
                                    currentFunctions.FirstOrDefault(
                                        func => func.Index <= caret1 && caret1 <= func.EndPos);

                            }
                        }
                    }
                }

                var smDef = Program.Configs[Program.SelectedConfig].GetSMDef()
                    .ProduceTemporaryExpandedDefinition(definitions, caret, currentFunctions);
                var smFunctions = smDef.Functions.ToArray();
                var acNodes = smDef.ProduceACNodes();
                var isNodes = smDef.ProduceISNodes();

                // Lags the hell out when typing a lot.
                ce.editor.SyntaxHighlighting = new AeonEditorHighlighting(smDef);

                foreach (var el in ee)
                {
                    if (el == ce)
                    {
                        Debug.Assert(ce != null, nameof(ce) + " != null");
                        if (ce.ISAC_Open)
                        {
                            continue;
                        }
                    }

                    
                    el.InterruptLoadAutoCompletes(smFunctions, acNodes,
                        isNodes, smDef.Methodmaps.ToArray());
                }
            });
        }

        public void UpdateFontSize(double size, bool updateLineHeight = true)
        {
            if (size > 2 && size < 31)
            {
                editor.FontSize = size;
                StatusLine_FontSize.Text = size.ToString("n0") + $" {Program.Translations.GetLanguage("PtAbb")}";
            }

            if (updateLineHeight)
            {
                LineHeight = editor.TextArea.TextView.DefaultLineHeight;
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
            {
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
                        var Result = await Program.MainWindow.ShowMessageAsync(title, "",
                            MessageDialogStyle.AffirmativeAndNegative, Program.MainWindow.MetroDialogOptions);
                        if (Result == MessageDialogResult.Affirmative)
                        {
                            Save();
                        }
                    }
                }
            }

            Program.MainWindow.EditorsReferences.Remove(this);

            Parent = null; //to prevent a ring depency which disables the GC from work
            Program.MainWindow.UpdateWindowTitle();
        }

        public void ToggleCommentOnLine()
        {
            var line = editor.Document.GetLineByOffset(editor.CaretOffset);
            var lineText = editor.Document.GetText(line);
            var leadingWhiteSpaces = 0;
            foreach (var l in lineText)
            {
                if (char.IsWhiteSpace(l))
                {
                    leadingWhiteSpaces++;
                }
                else
                {
                    break;
                }
            }

            lineText = lineText.Trim();
            if (lineText.Length > 1)
            {
                if (lineText[0] == '/' && lineText[1] == '/')
                {
                    editor.Document.Remove(line.Offset + leadingWhiteSpaces, 2);
                }
                else
                {
                    editor.Document.Insert(line.Offset + leadingWhiteSpaces, "//");
                }
            }
            else
            {
                editor.Document.Insert(line.Offset + leadingWhiteSpaces, "//");
            }
        }

        public void DuplicateLine(bool down)
        {
            var line = editor.Document.GetLineByOffset(editor.CaretOffset);
            var lineText = editor.Document.GetText(line);
            editor.Document.Insert(line.Offset, lineText + Environment.NewLine);
            if (down)
            {
                editor.CaretOffset -= line.Length + 1;
            }
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
                    var lineText = editor.Document.GetText(line.NextLine);
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
                    var lineText = editor.Document.GetText(line);
                    editor.Document.Remove(line.Offset, line.TotalLength);
                    editor.Document.Insert(insertOffset, lineText + Environment.NewLine);
                    editor.CaretOffset = insertOffset + relativeCaretOffset;
                }
            }
        }

        public void DeleteLine()
        {
            var line = editor.Document.GetLineByOffset(editor.CaretOffset);
            editor.Document.Remove(line.Offset, line.TotalLength);
        }

        private bool IsValidSearchSelectionString(string s)
        {
            var length = s.Length;
            for (var i = 0; i < length; ++i)
            {
                if (!((s[i] >= 'a' && s[i] <= 'z') || (s[i] >= 'A' && s[i] <= 'Z') || (s[i] >= '0' && s[i] <= '9') ||
                      s[i] == '_'))
                {
                    return false;
                }
            }

            return true;
        }

        public void Save(bool force = false)
        {
            if (_NeedsSave || force)
            {
                if (fileWatcher != null)
                {
                    fileWatcher.EnableRaisingEvents = false;
                }

                try
                {
                    using var fs = new FileStream(_FullFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    editor.Save(fs);
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
                if (fileWatcher != null)
                {
                    fileWatcher.EnableRaisingEvents = true;
                }
            }
        }

        public void StartAutoSaveTimer()
        {
            if (Program.OptionsObject.Editor_AutoSave)
            {
                if (AutoSaveTimer.Enabled)
                {
                    AutoSaveTimer.Stop();
                }

                AutoSaveTimer.Interval = 1000.0 * Program.OptionsObject.Editor_AutoSaveInterval;
                AutoSaveTimer.Start();
            }
        }

        public void Language_Translate(bool Initial = false)
        {
            if (Program.Translations.IsDefault)
            {
                return;
            }

            MenuC_Undo.Header = Program.Translations.GetLanguage("Undo");
            MenuC_Redo.Header = Program.Translations.GetLanguage("Redo");
            MenuC_Cut.Header = Program.Translations.GetLanguage("Cut");
            MenuC_Copy.Header = Program.Translations.GetLanguage("Copy");
            MenuC_Paste.Header = Program.Translations.GetLanguage("Paste");
            MenuC_SelectAll.Header = Program.Translations.GetLanguage("SelectAll");
            CompileBox.Content = Program.Translations.GetLanguage("Compile");
            if (!Initial)
            {
                StatusLine_Column.Text =
                    $"{Program.Translations.GetLanguage("ColAbb")} {editor.TextArea.Caret.Column}";
                StatusLine_Line.Text = $"{Program.Translations.GetLanguage("LnAbb")} {editor.TextArea.Caret.Line}";
                StatusLine_FontSize.Text =
                    editor.FontSize.ToString("n0") + $" {Program.Translations.GetLanguage("PtAbb")}";
            }
        }
        #endregion
    }

    public class ColorizeSelection : DocumentColorizingTransformer
    {
        public bool HighlightSelection;
        public string SelectionString = string.Empty;

        protected override void ColorizeLine(DocumentLine line)
        {
            if (HighlightSelection)
            {
                if (string.IsNullOrWhiteSpace(SelectionString))
                {
                    return;
                }

                var lineStartOffset = line.Offset;
                var text = CurrentContext.Document.GetText(line);
                var start = 0;
                int index;
                while ((index = text.IndexOf(SelectionString, start, StringComparison.Ordinal)) >= 0)
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