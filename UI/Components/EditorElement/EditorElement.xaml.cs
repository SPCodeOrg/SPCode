using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Utils;
using MahApps.Metro.Controls.Dialogs;
using SourcepawnCondenser;
using SourcepawnCondenser.SourcemodDefinition;
using SPCode.Utils;
using SPCode.Utils.SPSyntaxTidy;
using Xceed.Wpf.AvalonDock.Layout;
using static SPCode.Interop.TranslationProvider;
using Timer = System.Timers.Timer;

namespace SPCode.UI.Components;

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
    private bool SelectionIsHighlighted;
    private bool WantFoldingUpdate;
    public bool IsTemplateEditor = false;
    private bool Closed = false;
    public bool ClosingPromptOpened = false;

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

    /// <summary>
    /// This constructor calls its string-parametrized overload when sent from the templates window
    /// </summary>
    public EditorElement() : this(Program.SelectedTemplatePath)
    {
        IsTemplateEditor = true;
        InitializeComponent();
    }

    public EditorElement(string filePath)
    {
        InitializeComponent();

        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        bracketSearcher = new SPBracketSearcher();
        bracketHighlightRenderer = new BracketHighlightRenderer(editor.TextArea.TextView);
        editor.TextArea.IndentationStrategy = new EditorIndentationStrategy();

        editor.CaptureMouse();

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
        EvaluateRTL();
    }

    public void Editor_TabClosed(object sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Close();
    }

    private async void TextArea_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!Keyboard.IsKeyDown(Key.LeftCtrl) || IsTemplateEditor)
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
                    string.Format(Translate("DFileChanged"), _FullFilePath) +
                    Environment.NewLine + Translate("FileTryReload"),
                    Translate("FileChanged"), MessageBoxButton.YesNo,
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
                        Thread.Sleep(100);
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
                    SelectionIsHighlighted = true;
                    editor.TextArea.TextView.Redraw();
                }
                else
                {
                    colorizeSelection.HighlightSelection = false;
                    colorizeSelection.SelectionString = string.Empty;
                    if (SelectionIsHighlighted)
                    {
                        editor.TextArea.TextView.Redraw();
                        SelectionIsHighlighted = false;
                    }
                }
            }
            else
            {
                colorizeSelection.HighlightSelection = false;
                colorizeSelection.SelectionString = string.Empty;
                if (SelectionIsHighlighted)
                {
                    editor.TextArea.TextView.Redraw();
                    SelectionIsHighlighted = false;
                }
            }
        });
        if (WantFoldingUpdate)
        {
            WantFoldingUpdate = false;
            try
            {
                Dispatcher.Invoke(() => foldingStrategy.UpdateFoldings(foldingManager, editor.Document));
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
        if (Program.MainWindow == null)
        {
            return;
        }

        StatusLine_Column.Text = $"{Translate("ColAbb")} {editor.TextArea.Caret.Column}";
        StatusLine_Line.Text = $"{Translate("LnAbb")} {editor.TextArea.Caret.Line}";
#if DEBUG
        StatusLine_Offset.Text = $"Off {editor.TextArea.Caret.Offset}";
#endif
        var result = bracketSearcher.SearchBracket(editor.Document, editor.CaretOffset);
        bracketHighlightRenderer.SetHighlight(result);

        if (Program.OptionsObject.Program_DynamicISAC && editor.SelectionLength == 0)
        {
            EvaluateIntelliSense();
        }
        

        if (parseTimer != null)
        {
            parseTimer.Enabled = false;
            parseTimer.Close();
        }

        parseTimer = new Timer(200) {AutoReset = false, Enabled = true,};
        parseTimer.Elapsed += ParseIncludes;
    }

    public void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
    {
        if (Program.OptionsObject.Editor_ReformatLineAfterSemicolon)
        {
            if (e.Text == ";" && !BracketHelpers.CheckForString(editor.Document, editor.CaretOffset - 1))
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
                        var newLineStr = leadingIndentation + SPSyntaxTidy.TidyUp(text).Trim();
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

                editor.TextArea.Caret.Line--;
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
                        (BracketHelpers.CheckForCommentBlockForward(document, offset) ||
                         BracketHelpers.CheckForCommentBlockBackward(document, offset) ||
                         BracketHelpers.CheckForCommentLine(document, offset) ||
                         BracketHelpers.CheckForString(document, offset) ||
                         BracketHelpers.CheckForChar(document, offset)))
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
                        editor.CaretOffset--;
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
                    if (editor.SelectionLength > 0 ||
                        (lineText.Length > 0 && lineText[Math.Max(lineText.Length - 2, 0)] != '\\'))
                    {
                        editor.Document.Insert(editor.CaretOffset, e.Text);
                        if (editor.SelectionLength == 0)
                        {
                            editor.CaretOffset--;
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

            case ")":
            case "]":
            case "}":
                if (Program.OptionsObject.Editor_AutoCloseBrackets)
                {
                    if (editor.TextArea.Caret.Offset < editor.Document.TextLength &&
                        BracketHelpers.CheckForClosingBracket(editor.Document, editor.TextArea.Caret.Offset,
                            e.Text))
                    {
                        e.Handled = true;
                        var newCaretPos = editor.TextArea.Caret.Offset + 1;
                        var docLength = editor.Document.TextLength;
                        editor.TextArea.Caret.Offset = newCaretPos > docLength ? docLength : newCaretPos;
                    }
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
        StatusLine_SelectionLength.Text = $"{Translate("LenAbb")} {editor.SelectionLength}";
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
            e.Handled = true;
        }

        HideTooltip();
    }

    private void Editor_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        HideTooltip();
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

        if (!HotkeyUtils.IsKeyModifier(key))
        {
            Program.MainWindow.ProcessHotkey(new Hotkey(key, modifiers), e);
        }
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

    private static readonly Regex IncludeRegex = new("\\s*#(?:try)?include\\s+\"(.+)\"",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private void ParseIncludes(object sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (Program.MainWindow == null)
            {
                return;
            }

            var editors = Program.MainWindow.EditorReferences;
            var currentEditor = Program.MainWindow.GetCurrentEditorElement();

            var caret = -1;

            if (!editors.Any() || currentEditor == null)
            {
                return;
            }

            var definitions = new List<SMDefinition>(editors.Count);

            List<SMFunction> currentFunctions = null;

            foreach (var el in editors)
            {
                var fInfo = new FileInfo(el.FullFilePath);
                var text = el.editor.Document.Text;
                if (fInfo.Extension.Trim('.').ToLowerInvariant() == "inc")
                {
                    definitions.Add(new Condenser(text, fInfo.FullName).Condense());
                }

                if (fInfo.Extension.Trim('.').ToLowerInvariant() != "sp" || !el.IsLoaded)
                    continue;

                caret = el.editor.CaretOffset;
                var def = new Condenser(text, fInfo.FullName).Condense();
                definitions.Add(def);


                if (el != currentEditor)
                    continue;

                currentFunctions = def.Functions;

                var matches = IncludeRegex.Matches(text);
                var files = new HashSet<string>();

                foreach (Match match in matches)
                {
                    files.Add(match.Groups[1].Value);
                }

                foreach (var file in files)
                {
                    var path = Path.Combine(new[] {Path.GetDirectoryName(el.FullFilePath), file});
                    var fileInfo = new FileInfo(path);
                    if (!fileInfo.Exists)
                        continue;

                    var fileText = File.ReadAllText(path);
                    var condensedFile = new Condenser(fileText, fileInfo.FullName).Condense();

                    def.MergeDefinitions(condensedFile);
                }

                _currentSMDef = def;
                
                var caret1 = caret;
                def.CurrentFunction = currentFunctions.FirstOrDefault(func => func.Index <= caret1 && caret1 <= func.EndPos);
            }

            var smDef = Program.Configs[Program.SelectedConfig].GetSMDef()
                .ProduceTemporaryExpandedDefinition(definitions, caret, currentFunctions);

            // Lags the hell out when typing a lot.
            currentEditor.editor.SyntaxHighlighting = new AeonEditorHighlighting(smDef);

            foreach (var el in editors)
            {
                if (el == currentEditor)
                {
                    Debug.Assert(currentEditor != null, nameof(currentEditor) + " != null");
                    if (currentEditor._isTooltipOpen)
                    {
                        continue;
                    }
                }

                el.InterruptLoadAutoCompletes(smDef);
            }
        });
    }

    public void UpdateFontSize(double size, bool updateLineHeight = true)
    {
        if (size > 2 && size < 31)
        {
            editor.FontSize = size;
            StatusLine_FontSize.Text = size.ToString("n0") + $" {Translate("PtAbb")}";
        }

        if (updateLineHeight)
        {
            LineHeight = editor.TextArea.TextView.DefaultLineHeight;
        }
    }

    public async void Close(bool ForcedToSave = false, bool CheckSavings = true)
    {
        var action = Program.OptionsObject.ActionOnClose;

        if (CheckSavings && _NeedsSave && !Closed && action != ActionOnClose.DontSave)
        {
            if (ForcedToSave || action == ActionOnClose.Save)
            {
                Save();
            }
            else
            {
                ClosingPromptOpened = true;
                var result = await Program.MainWindow.ShowMessageAsync(
                    $"Do you want to save changes to '{Parent.Title.Substring(1)}'?",
                    "Your changes will be lost if you don't save them",
                    MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary,
                    Program.MainWindow.ClosingDialogOptions);
                ClosingPromptOpened = false;
                switch (result)
                {
                    case MessageDialogResult.Affirmative:
                        Closed = true;
                        Save();
                        break;
                    case MessageDialogResult.Negative:
                        Closed = true;
                        break;
                    case MessageDialogResult.FirstAuxiliary:
                        return;
                }
            }
        }

        Program.MainWindow.DockingPane.RemoveChild(Parent);
        Program.MainWindow.UpdateOBFileButton();

        regularyTimer.Stop();
        regularyTimer.Close();

        if (fileWatcher != null)
        {
            fileWatcher.EnableRaisingEvents = false;
            fileWatcher.Dispose();
            fileWatcher = null;
        }

        Parent = null;

        Program.MainWindow.EditorReferences.Remove(this);
        Program.MainWindow.MenuI_ReopenLastClosedTab.IsEnabled = true;
        Program.RecentFilesStack.Push(FullFilePath);
        Program.MainWindow.UpdateWindowTitle();
    }

    public void ToggleComment(bool comment)
    {
        // Get the selection segments
        var selectionSegments = editor.TextArea.Selection.Segments;

        var lineList = new List<DocumentLine>();
        var document = editor.TextArea.Document;

        // Start update so undoing this doesn't result in undoing every single comment manually
        document.BeginUpdate();

        // If there's no selection, add to lineList the line the caret is standing on
        if (!selectionSegments.Any())
        {
            lineList.Add(document.GetLineByOffset(editor.TextArea.Caret.Offset));
        }
        else
        {
            // Get all the lines from that selection and store them in a list
            foreach (var segment in selectionSegments)
            {
                var lineStart = document.GetLineByOffset(segment.StartOffset).LineNumber;
                var lineEnd = document.GetLineByOffset(segment.EndOffset).LineNumber;
                for (var i = lineStart; i <= lineEnd; i++)
                {
                    lineList.Add(editor.Document.GetLineByNumber(i));
                }
            }
        }

        // For each line, apply comment logic
        foreach (var line in lineList)
        {
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
                if (!comment && lineText[0] == '/' && lineText[1] == '/')
                {
                    editor.Document.Remove(line.Offset + leadingWhiteSpaces, 2);
                }
                else if (comment && lineText[0] != '/' && lineText[1] != '/')
                {
                    editor.Document.Insert(line.Offset + leadingWhiteSpaces, "//");
                }
            }
            else if (comment)
            {
                editor.Document.Insert(line.Offset + leadingWhiteSpaces, "//");
            }
        }

        // End update
        document.EndUpdate();
    }

    public void ChangeCase(bool toUpper = true)
    {
        var selection = editor.TextArea.Selection;
        if (!selection.IsEmpty)
        {
            var newText = toUpper ? selection.GetText().ToUpperInvariant() : selection.GetText().ToLowerInvariant();
            selection.ReplaceSelectionWithText(newText);
            var startOffset =
                editor.TextArea.Document.GetOffset(selection.StartPosition.Line, selection.StartPosition.Column);
            var endOffset =
                editor.TextArea.Document.GetOffset(selection.EndPosition.Line, selection.EndPosition.Column);
            editor.TextArea.Selection = Selection.Create(editor.TextArea, startOffset, endOffset);
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
                    Translate("DSaveError") + Environment.NewLine + "(" + e.Message + ")",
                    Translate("SaveError"),
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
        MenuC_Undo.Header = Translate("Undo");
        MenuC_Redo.Header = Translate("Redo");
        MenuC_Cut.Header = Translate("Cut");
        MenuC_Copy.Header = Translate("Copy");
        MenuC_Paste.Header = Translate("Paste");
        MenuC_SelectAll.Header = Translate("SelectAll");
        CompileBox.Content = Translate("Compile");
        DontCreateFileBox.Content = Translate("DontOutputFile");
        if (!Initial)
        {
            StatusLine_Column.Text =
                $"{Translate("ColAbb")} {editor.TextArea.Caret.Column}";
            StatusLine_Line.Text = $"{Translate("LnAbb")} {editor.TextArea.Caret.Line}";
            StatusLine_FontSize.Text =
                editor.FontSize.ToString("n0") + $" {Translate("PtAbb")}";
        }
    }

    public void EvaluateRTL()
    {
        TooltipGrid.FlowDirection = FlowDirection.LeftToRight;
    }

    #endregion
}