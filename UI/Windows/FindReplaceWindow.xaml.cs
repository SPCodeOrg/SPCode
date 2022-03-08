using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro;
using SPCode.UI.Components;
using Xceed.Wpf.AvalonDock.Layout;
using static SPCode.Interop.TranslationProvider;

namespace SPCode.UI.Windows
{
    public partial class FindReplaceWindow
    {
        #region Variables
        private EditorElement _editor;
        private List<EditorElement> _allEditors;
        private LayoutDocumentPane _dockingPane;
        private bool IsSearchFieldOpen;
        private readonly string Selection;
        private readonly SearchOptions _searchOptions;
        private readonly bool AllowChanging = false;

        private readonly ObservableCollection<string> FindReplaceButtonItems = new()
        {
            Translate("Replace"),
            Translate("ReplaceAll")
        };

        private enum RadioButtons
        {
            NSearch_RButton = 1,
            WSearch_RButton = 2,
            ASearch_RButton = 3,
            RSearch_RButton = 4
        }

        private enum DocumentType
        {
            MenuFR_CurrDoc = 0,
            MenuFR_AllDoc = 1
        }
        #endregion

        #region Constructors
        public FindReplaceWindow(string searchTerm = "")
        {
            InitializeComponent();
            if (Program.OptionsObject.Program_AccentColor != "Red" || Program.OptionsObject.Program_Theme != "BaseDark")
            {
                ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor),
                    ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme));
            }

            _searchOptions = Program.OptionsObject.SearchOptions;
            Left = Program.MainWindow.Left + Program.MainWindow.Width - Program.MainWindow.ObjectBrowserColumn.Width.Value - (Width + 20);
            Top = Program.MainWindow.Top + 40;

            Selection = searchTerm;
            ReplaceButton.ItemsSource = FindReplaceButtonItems;

            RestoreSettings();
            LoadEditorsInfo();
            Language_Translate();
            EvaluateRTL();

            FindBox.SelectAll();
            AllowChanging = true;
        }
        #endregion

        #region Events
        private void CloseFindReplaceGrid(object sender, RoutedEventArgs e)
        {
            ToggleSearchField();
        }

        private void SearchButtonClicked(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void ReplaceButtonClicked(object sender, RoutedEventArgs e)
        {
            if (ReplaceButton.SelectedIndex == 1)
            {
                ReplaceAll();
            }
            else
            {
                Replace();
            }
        }

        private void CountButtonClicked(object sender, RoutedEventArgs e)
        {
            Count();
        }

        private void SearchBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Search();
            }
        }

        private void ReplaceBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Replace();
            }
        }

        private void FindReplaceGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ToggleSearchField();
            }
        }

        private void MetroWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    break;
                case Key.F3:
                    Search();
                    break;
            }
        }

        private void SearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChanging) return;
            Program.OptionsObject.SearchOptions.FindText = FindBox.Text;
            FindResultBlock.Text = string.Empty;
        }

        private void ReplaceBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChanging) return;
            Program.OptionsObject.SearchOptions.ReplaceText = ReplaceBox.Text;
        }

        private void RadioButtonsChanged(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging) return;
            var button = sender as RadioButton;
            Program.OptionsObject.SearchOptions.SearchType = (int)Enum.Parse(typeof(RadioButtons), button.Name);
        }

        private void DocumentChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChanging) return;
            var item = (sender as ComboBox).SelectedItem as ComboBoxItem;
            Program.OptionsObject.SearchOptions.Document = (int)Enum.Parse(typeof(DocumentType), item.Name);
        }

        private void MultilineRegexChanged(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging) return;
            var checkbox = sender as CheckBox;
            Program.OptionsObject.SearchOptions.MultilineRegex = checkbox.IsChecked.Value;
        }

        private void CaseSensitiveChanged(object sender, RoutedEventArgs e)
        {
            if (!AllowChanging) return;
            var checkbox = sender as CheckBox;
            Program.OptionsObject.SearchOptions.CaseSensitive = checkbox.IsChecked.Value;
        }

        private void ReplaceChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChanging) return;
            Program.OptionsObject.SearchOptions.ReplaceType = ReplaceButton.SelectedIndex;
        }
        #endregion

        #region Methods
        private void ToggleSearchField()
        {
            LoadEditorsInfo();
            if (IsSearchFieldOpen)
            {
                if (_editor != null)
                {
                    if (_editor.IsKeyboardFocusWithin)
                    {
                        if (_editor.editor.SelectionLength > 0)
                        {
                            FindBox.Text = _editor.editor.SelectedText;
                        }
                        FindBox.SelectAll();
                        FindBox.Focus();
                        return;
                    }
                }
                IsSearchFieldOpen = false;
                FindReplaceGrid.IsHitTestVisible = false;
                if (_editor == null)
                {
                    return;
                }
                _editor.editor.Focus();
            }
            else
            {
                IsSearchFieldOpen = true;
                FindReplaceGrid.IsHitTestVisible = true;
                if (_editor == null)
                {
                    return;
                }
                if (_editor.editor.SelectionLength > 0)
                {
                    FindBox.Text = _editor.editor.SelectedText;
                }
                FindBox.Focus();
                FindBox.SelectAll();
            }
        }

        private void Search()
        {
            LoadEditorsInfo();
            var editors = GetEditorElementsForFraction(out var editorIndex);
            var regex = GetSearchRegex();
            if (editors == null || editors.Count < 1 || editors[0] == null || regex == null)
            {
                return;
            }

            var startFileCaretOffset = 0;
            var foundOccurence = false;

            for (var i = editorIndex; i < (editors.Count + editorIndex + 1); ++i)
            {
                var index = ValueUnderMap(i, editors.Count);
                string searchText;
                var addToOffset = 0;
                if (i == editorIndex)
                {
                    startFileCaretOffset = editors[index].editor.CaretOffset;
                    addToOffset = startFileCaretOffset;
                    if (startFileCaretOffset < 0) { startFileCaretOffset = 0; }
                    searchText = editors[index].editor.Text.Substring(startFileCaretOffset);
                }
                else if (i == (editors.Count + editorIndex))
                {
                    searchText = startFileCaretOffset == 0 ?
                        string.Empty :
                        editors[index].editor.Text.Substring(0, startFileCaretOffset);
                }
                else
                {
                    searchText = editors[index].editor.Text;
                }
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var m = regex.Match(searchText);
                    if (m.Success) // can this happen ?
                    {
                        foundOccurence = true;
                        editors[index].Parent.IsSelected = true;
                        editors[index].editor.CaretOffset = m.Index + addToOffset + m.Length;
                        editors[index].editor.Select(m.Index + addToOffset, m.Length);
                        var location = editors[index].editor.Document.GetLocation(m.Index + addToOffset);
                        editors[index].editor.ScrollTo(location.Line, location.Column);
                        FindResultBlock.Text = string.Format(Translate("FoundInOff"), m.Index + addToOffset, m.Length);
                        break;
                    }
                }
            }
            if (!foundOccurence)
            {
                FindResultBlock.Text = Translate("FoundNothing");
            }
        }

        private void Replace()
        {
            LoadEditorsInfo();
            var editors = GetEditorElementsForFraction(out var editorIndex);
            var regex = GetSearchRegex();
            if (editors == null || editors.Count < 1 || editors[0] == null || regex == null)
            {
                return;
            }

            var replaceString = ReplaceBox.Text;
            var startFileCaretOffset = 0;
            var foundOccurence = false;
            for (var i = editorIndex; i < (editors.Count + editorIndex + 1); ++i)
            {
                var index = ValueUnderMap(i, editors.Count);
                string searchText;
                var addToOffset = 0;
                if (i == editorIndex)
                {
                    startFileCaretOffset = editors[index].editor.CaretOffset;
                    addToOffset = startFileCaretOffset;
                    if (startFileCaretOffset < 0) { startFileCaretOffset = 0; }
                    searchText = editors[index].editor.Text.Substring(startFileCaretOffset);
                }
                else if (i == (editors.Count + editorIndex))
                {
                    searchText = startFileCaretOffset == 0 ?
                        string.Empty :
                        editors[index].editor.Text.Substring(0, startFileCaretOffset);
                }
                else
                {
                    searchText = editors[index].editor.Text;
                }
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var m = regex.Match(searchText);
                    if (m.Success)
                    {
                        foundOccurence = true;
                        editors[index].Parent.IsSelected = true;
                        var result = m.Result(replaceString);
                        editors[index].editor.Document.Replace(m.Index + addToOffset, m.Length, result);
                        editors[index].editor.CaretOffset = m.Index + addToOffset + result.Length;
                        editors[index].editor.Select(m.Index + addToOffset, result.Length);
                        var location = editors[index].editor.Document.GetLocation(m.Index + addToOffset);
                        editors[index].editor.ScrollTo(location.Line, location.Column);
                        FindResultBlock.Text = string.Format(Translate("ReplacedOff"), MinHeight + addToOffset);
                        break;
                    }
                }
            }
            if (!foundOccurence)
            {
                FindResultBlock.Text = Translate("FoundNothing");
            }
        }

        private void ReplaceAll()
        {
            LoadEditorsInfo();
            var editors = GetEditorElementsForFraction(out _);
            var regex = GetSearchRegex();
            if (editors == null || editors.Count < 1 || editors[0] == null || regex == null)
            {
                return;
            }

            var count = 0;
            var fileCount = 0;

            var replaceString = ReplaceBox.Text;
            foreach (var editor in editors)
            {
                var mc = regex.Matches(editor.editor.Text);
                if (mc.Count > 0)
                {
                    fileCount++;
                    count += mc.Count;
                    editor.editor.BeginChange();
                    for (var j = mc.Count - 1; j >= 0; --j)
                    {
                        var replace = mc[j].Result(replaceString);
                        editor.editor.Document.Replace(mc[j].Index, mc[j].Length, replace);
                    }
                    editor.editor.EndChange();
                    editor.NeedsSave = true;
                }
            }
            FindResultBlock.Text = string.Format(Translate("ReplacedOcc"), count, fileCount);
        }

        private void Count()
        {
            LoadEditorsInfo();
            var editors = GetEditorElementsForFraction(out _);
            if (editors == null) { return; }
            if (editors.Count < 1) { return; }
            if (editors[0] == null) { return; }
            var regex = GetSearchRegex();
            if (regex == null) { return; }
            var count = 0;
            foreach (var editor in editors)
            {
                var mc = regex.Matches(editor.editor.Text);
                count += mc.Count;
            }
            FindResultBlock.Text = count.ToString() + " " + Translate("OccFound");
        }

        private Regex GetSearchRegex()
        {
            var findString = FindBox.Text;
            if (string.IsNullOrEmpty(findString))
            {
                FindResultBlock.Text = Translate("EmptyPatt");
                return null;
            }
            Regex regex = new(string.Empty);
            var regexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant;
            Debug.Assert(CCBox.IsChecked != null, "CCBox.IsChecked != null");
            Debug.Assert(NSearch_RButton.IsChecked != null, "NSearch_RButton.IsChecked != null");


            if (!CCBox.IsChecked.Value)
            { regexOptions |= RegexOptions.IgnoreCase; }

            if (NSearch_RButton.IsChecked.Value)
            {
                regex = new Regex(Regex.Escape(findString), regexOptions);
            }
            else
            {
                Debug.Assert(WSearch_RButton.IsChecked != null, "WSearch_RButton.IsChecked != null");
                if (WSearch_RButton.IsChecked.Value)
                {
                    regex = new Regex("\\b" + Regex.Escape(findString) + "\\b", regexOptions);
                }
                else
                {
                    Debug.Assert(ASearch_RButton.IsChecked != null, "ASearch_RButton.IsChecked != null");
                    if (ASearch_RButton.IsChecked.Value)
                    {
                        findString = findString.Replace("\\t", "\t").Replace("\\r", "\r").Replace("\\n", "\n");
                        var rx = new Regex(@"\\[uUxX]([0-9A-F]{4})");
                        findString = rx.Replace(findString,
                            match => ((char)int.Parse(match.Value.Substring(2), NumberStyles.HexNumber)).ToString());
                        regex = new Regex(Regex.Escape(findString), regexOptions);
                    }
                    else if (RSearch_RButton.IsChecked.Value)
                    {
                        regexOptions |= RegexOptions.Multiline;
                        Debug.Assert(MLRBox.IsChecked != null, "MLRBox.IsChecked != null");
                        if (MLRBox.IsChecked.Value)
                        { regexOptions |= RegexOptions.Singleline; }
                        // paradox, isn't it? ^^
                        try
                        {
                            regex = new Regex(findString, regexOptions);
                        }
                        catch (Exception)
                        {
                            FindResultBlock.Text = Translate("NoValidRegex"); return null;
                        }
                    }
                }
            }

            return regex;
        }

        private List<EditorElement> GetEditorElementsForFraction(out int editorIndex)
        {
            LoadEditorsInfo();
            var editorStartIndex = 0;
            List<EditorElement> editors;
            if (FindDestinies.SelectedIndex == 0)
            { editors = new() { _editor }; }
            else
            {
                editors = _allEditors;
                var checkElement = _dockingPane.SelectedContent?.Content;
                if (checkElement is EditorElement)
                {
                    for (var i = 0; i < editors.Count; ++i)
                    {
                        if (editors[i] == checkElement)
                        {
                            editorStartIndex = i;
                        }
                    }
                }
            }
            editorIndex = editorStartIndex;
            return editors;
        }

        private int ValueUnderMap(int value, int map)
        {
            while (value >= map)
            {
                value -= map;
            }
            return value;
        }

        private void LoadEditorsInfo()
        {
            _editor = Program.MainWindow.GetCurrentEditorElement();
            _allEditors = Program.MainWindow.GetAllEditorElements();
            _dockingPane = Program.MainWindow.DockingPane;
        }

        private void RestoreSettings()
        {
            // Restore find and replace texts
            FindBox.Text = string.IsNullOrEmpty(Selection) ? _searchOptions.FindText : Selection;
            ReplaceBox.Text = _searchOptions.ReplaceText;

            // Restore radio buttons 
            var AllRadioButtons = new RadioButton[]
            {
                NSearch_RButton,
                WSearch_RButton,
                ASearch_RButton,
                RSearch_RButton
            };

            var stype = _searchOptions.SearchType;
            var index = stype - 1 == -1 ? stype : stype - 1;
            AllRadioButtons[index].IsChecked = true;

            // Restore documents
            FindDestinies.SelectedIndex = _searchOptions.Document;

            // Restore checkboxes
            CCBox.IsChecked = _searchOptions.CaseSensitive;
            MLRBox.IsChecked = _searchOptions.MultilineRegex;

            // Restore replace button
            ReplaceButton.SelectedIndex = _searchOptions.ReplaceType;
        }

        public void Language_Translate()
        {
            Title = Translate("Search");
            NSearch_RButton.Content = Translate("NormalSearch");
            WSearch_RButton.Content = Translate("MatchWholeWords");
            ASearch_RButton.Content = Translate("AdvancSearch");
            ASearch_RButton.ToolTip = Translate("AdvancSearchDetails");
            RSearch_RButton.Content = Translate("RegexSearch");
            MenuFR_CurrDoc.Content = Translate("CurrDoc");
            MenuFR_AllDoc.Content = Translate("AllDoc");

            Find_Button.Content = Translate("Find");
            Count_Button.Content = Translate("Count");
            CCBoxText.Text = Translate("CaseSen");
            MLRBoxText.Text = Translate("MultilineRegex");
        }

        private void EvaluateRTL()
        {
            FindReplaceGrid.FlowDirection = Program.IsRTL ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }
        #endregion
    }
}