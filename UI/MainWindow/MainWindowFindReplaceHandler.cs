using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using SPCode.UI.Components;

namespace SPCode.UI
{
    public partial class MainWindow
    {
        private bool IsSearchFieldOpen;

        private void ToggleSearchField()
        {
            var ee = GetCurrentEditorElement();
            if (IsSearchFieldOpen)
            {
                if (ee != null)
                {
                    if (ee.IsKeyboardFocusWithin)
                    {
                        if (ee.editor.SelectionLength > 0)
                        {
                            FindBox.Text = ee.editor.SelectedText;
                        }
                        FindBox.SelectAll();
                        FindBox.Focus();
                        return;
                    }
                }
                IsSearchFieldOpen = false;
                FindReplaceGrid.IsHitTestVisible = false;
                if (Program.OptionsObject.UI_Animations)
                {
                    FadeFindReplaceGridOut.Begin();
                }
                else
                {
                    FindReplaceGrid.Opacity = 0.0;
                }
                if (ee == null)
                {
                    return;
                }
                ee.editor.Focus();
            }
            else
            {
                IsSearchFieldOpen = true;
                FindReplaceGrid.IsHitTestVisible = true;
                if (ee == null)
                {
                    return;
                }
                if (ee.editor.SelectionLength > 0)
                {
                    FindBox.Text = ee.editor.SelectedText;
                }
                FindBox.SelectAll();
                if (Program.OptionsObject.UI_Animations)
                {
                    FadeFindReplaceGridIn.Begin();
                }
                else
                {
                    FindReplaceGrid.Opacity = 1.0;
                }
                FindBox.Focus();
            }
        }

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
        private void SearchBoxTextChanged(object sender, RoutedEventArgs e)
        {
            FindResultBlock.Text = string.Empty;
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

        private void Search()
        {
            var editors = GetEditorElementsForFRAction(out var editorIndex);
            if (editors == null) { return; }
            if (editors.Length < 1) { return; }
            if (editors[0] == null) { return; }
            var regex = GetSearchRegex();
            if (regex == null) { return; }
            var startFileCaretOffset = 0;
            var foundOccurence = false;
            for (var i = editorIndex; i < (editors.Length + editorIndex + 1); ++i)
            {
                var index = ValueUnderMap(i, editors.Length);
                string searchText;
                var addToOffset = 0;
                if (i == editorIndex)
                {
                    startFileCaretOffset = editors[index].editor.CaretOffset;
                    addToOffset = startFileCaretOffset;
                    if (startFileCaretOffset < 0) { startFileCaretOffset = 0; }
                    searchText = editors[index].editor.Text.Substring(startFileCaretOffset);
                }
                else if (i == (editors.Length + editorIndex))
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
                    if (m.Success) //can this happen?
                    {
                        foundOccurence = true;
                        editors[index].Parent.IsSelected = true;
                        editors[index].editor.CaretOffset = m.Index + addToOffset + m.Length;
                        editors[index].editor.Select(m.Index + addToOffset, m.Length);
                        var location = editors[index].editor.Document.GetLocation(m.Index + addToOffset);
                        editors[index].editor.ScrollTo(location.Line, location.Column);
                        //FindResultBlock.Text = "Found in offset " + (m.Index + addToOffset).ToString() + " with length " + m.Length.ToString();
                        FindResultBlock.Text = string.Format(Program.Translations.GetLanguage("FoundInOff"), m.Index + addToOffset, m.Length);
                        break;
                    }
                }
            }
            if (!foundOccurence)
            {
                FindResultBlock.Text = Program.Translations.GetLanguage("FoundNothing");
            }
        }

        private void Replace()
        {
            var editors = GetEditorElementsForFRAction(out var editorIndex);
            if (editors == null) { return; }
            if (editors.Length < 1) { return; }
            if (editors[0] == null) { return; }
            var regex = GetSearchRegex();
            if (regex == null) { return; }
            var replaceString = ReplaceBox.Text;
            var startFileCaretOffset = 0;
            var foundOccurence = false;
            for (var i = editorIndex; i < (editors.Length + editorIndex + 1); ++i)
            {
                var index = ValueUnderMap(i, editors.Length);
                string searchText;
                var addToOffset = 0;
                if (i == editorIndex)
                {
                    startFileCaretOffset = editors[index].editor.CaretOffset;
                    addToOffset = startFileCaretOffset;
                    if (startFileCaretOffset < 0) { startFileCaretOffset = 0; }
                    searchText = editors[index].editor.Text.Substring(startFileCaretOffset);
                }
                else if (i == (editors.Length + editorIndex))
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
                        FindResultBlock.Text = string.Format(Program.Translations.GetLanguage("ReplacedOff"), MinHeight + addToOffset);
                        break;
                    }
                }
            }
            if (!foundOccurence)
            {
                FindResultBlock.Text = Program.Translations.GetLanguage("FoundNothing");
            }
        }

        private void ReplaceAll()
        {
            var editors = GetEditorElementsForFRAction(out _);
            if (editors == null) { return; }
            if (editors.Length < 1) { return; }
            if (editors[0] == null) { return; }
            var regex = GetSearchRegex();
            if (regex == null) { return; }

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
            // FindResultBlock.Text = "Replaced " + count.ToString() + " occurences in " + fileCount.ToString() + " documents";
            FindResultBlock.Text = string.Format(Program.Translations.GetLanguage("ReplacedOcc"), count, fileCount);
        }

        private void Count()
        {
            var editors = GetEditorElementsForFRAction(out _);
            if (editors == null) { return; }
            if (editors.Length < 1) { return; }
            if (editors[0] == null) { return; }
            var regex = GetSearchRegex();
            if (regex == null) { return; }
            var count = 0;
            foreach (var editor in editors)
            {
                var mc = regex.Matches(editor.editor.Text);
                count += mc.Count;
            }
            FindResultBlock.Text = count.ToString() + " " + Program.Translations.GetLanguage("OccFound");
        }

        private Regex GetSearchRegex()
        {
            var findString = FindBox.Text;
            if (string.IsNullOrEmpty(findString))
            {
                FindResultBlock.Text = Program.Translations.GetLanguage("EmptyPatt");
                return null;
            }
            Regex regex;
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
                    else //if (RSearch_RButton.IsChecked.Value)
                    {
                        regexOptions |= RegexOptions.Multiline;
                        Debug.Assert(MLRBox.IsChecked != null, "MLRBox.IsChecked != null");
                        if (MLRBox.IsChecked.Value)
                        { regexOptions |= RegexOptions.Singleline; } //paradox, isn't it? ^^
                        try
                        {
                            regex = new Regex(findString, regexOptions);
                        }
                        catch (Exception) { FindResultBlock.Text = Program.Translations.GetLanguage("NoValidRegex"); return null; }
                    }
                }
            }

            return regex;
        }

        private EditorElement[] GetEditorElementsForFRAction(out int editorIndex)
        {
            var editorStartIndex = 0;
            EditorElement[] editors;
            if (FindDestinies.SelectedIndex == 0)
            { editors = new[] { GetCurrentEditorElement() }; }
            else
            {
                editors = GetAllEditorElements();
                var checkElement = DockingPane.SelectedContent?.Content;
                if (checkElement is EditorElement)
                {
                    for (var i = 0; i < editors.Length; ++i)
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
    }
}
