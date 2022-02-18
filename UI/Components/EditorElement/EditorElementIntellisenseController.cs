using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.SqlServer.Server;
using SourcepawnCondenser.SourcemodDefinition;

namespace SPCode.UI.Components
{
    /// @note
    // AC stands for AutoComplete and ShowAC is used to show a the autocomplete dialog.
    // IS stands for IntelliSense and ShowIS is used to show an object documentation.
    // You always need to call ShowTooltip to actually Show the dialog.
    public partial class EditorElement
    {
        private bool AC_IsFuncC = true;
        private bool _isAcOpen;
        private List<ACNode> _acEntries;

        private bool _animationsLoaded;

        private Storyboard _fadeFuncACIn;
        private Storyboard _fadeMethodACIn;
        private Storyboard _fadePreProcACIn;

        private Storyboard _fadeACIn;
        private Storyboard _fadeACOut;
        private Storyboard _fadeTooltipIn;
        private Storyboard _fadeTooltipOut;

        private SMFunction[] _func;
        private bool _isDocOpen;
        private bool _isTooltipOpen;
        private List<ISNode> _docEntries;

        // Used to keep track of the current autocomplete type (ie. toplevel, class or preprocessor)
        private ACType _acType = ACType.Toplevel;


        private readonly Regex ISFindRegex = new(
            @"\b((?<class>[a-zA-Z_]([a-zA-Z0-9_]?)+)\.(?<method>[a-zA-Z_]([a-zA-Z0-9_]?)+)\()|((?<name>[a-zA-Z_]([a-zA-Z0-9_]?)+)\()",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        private int LastShowedLine = -1;

        // TODO Add EnumStructs
        private SMMethodmap[] methodMaps;

        private readonly Regex multilineCommentRegex = new(@"/\*.*?\*/",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        // Pre-processor statements
        static private readonly string[] PreProcArr =
        {
            "assert", "define", "else", "elseif", "endif", "endinput", "endscript", "error", "warning", "if",
            "include", "line", "pragma", "tryinclude", "undef"
        };

        static private readonly List<string> PreProcList = PreProcArr.ToList();

        static private readonly Regex PreprocessorRegex = new("#\\w+");

        //private string[] methodNames;
        public void LoadAutoCompletes()
        {
            if (!_animationsLoaded)
            {
                _fadeTooltipIn = (Storyboard)Resources["FadeISACIn"];
                _fadeTooltipOut = (Storyboard)Resources["FadeISACOut"];

                _fadeACIn = (Storyboard)Resources["FadeACIn"];
                _fadeACOut = (Storyboard)Resources["FadeACOut"];

                _fadeFuncACIn = (Storyboard)Resources["FadeAC_FuncC_In"];
                _fadeMethodACIn = (Storyboard)Resources["FadeAC_MethodC_In"];
                _fadePreProcACIn = (Storyboard)Resources["FadeAC_PreProc_In"];

                _fadeTooltipOut.Completed += FadeTooltipOutCompleted;
                _fadeACOut.Completed += FadeACOut_Completed;

                _animationsLoaded = true;
            }

            if (_isTooltipOpen)
            {
                HideTooltip();
            }

            var def = Program.Configs[Program.SelectedConfig].GetSMDef();
            _func = def.Functions.ToArray();
            _acEntries = def.ProduceACNodes();
            _docEntries = def.ProduceISNodes();
            methodMaps = def.Methodmaps.ToArray();

            AutoCompleteBox.ItemsSource = _acEntries;
            MethodAutoCompleteBox.ItemsSource = _docEntries;
            PreProcAutocompleteBox.ItemsSource = ACNode.ConvertFromStringList(PreProcList, false, "#", true);
        }

        private void InterruptLoadAutoCompletes(SMFunction[] FunctionArray, List<ACNode> acNodes,
            List<ISNode> isNodes, SMMethodmap[] newMethodMaps)
        {
            Dispatcher?.Invoke(() =>
            {
                _func = FunctionArray;
                _acEntries = acNodes;
                _docEntries = isNodes;
                AutoCompleteBox.ItemsSource = _acEntries;
                MethodAutoCompleteBox.ItemsSource = _docEntries;
                PreProcAutocompleteBox.ItemsSource = ACNode.ConvertFromStringList(PreProcList, false, "#", true);
                methodMaps = newMethodMaps;
            });
        }

        //private readonly Regex methodExp = new Regex(@"(?<=\.)[A-Za-z_]\w*", RegexOptions.RightToLeft);
        private void EvaluateIntelliSense(out bool refresh)
        {
            refresh = false;

            if (editor.SelectionLength > 0)
            {
                HideTooltip();
                return;
            }

            var currentLineIndex = editor.TextArea.Caret.Line - 1;
            var line = editor.Document.Lines[currentLineIndex];
            var text = editor.Document.GetText(line.Offset, line.Length);
            var lineOffset = editor.TextArea.Caret.Column - 1;
            var caretOffset = editor.CaretOffset;

            // Keep track of the last line
            LastShowedLine = currentLineIndex;
            //todo: Implement ForceIsKeepsClosed

            var xPos = int.MaxValue;

            var quotationCount = 0;


            // Hide tooltip if inside in-line comments.
            for (var i = 0; i < lineOffset; ++i)
            {
                // Exclude the cases where there are two "//" inside a string.
                if (text[i] == '"')
                {
                    if (i != 0)
                    {
                        if (text[i - 1] != '\\')
                        {
                            quotationCount++;
                        }
                    }
                }

                if (quotationCount % 2 != 0 || text[i] != '/' || i == 0 || text[i - 1] != '/')
                {
                    continue;
                }

                HideTooltip();
                return;
            }


            // Hide Tooltip if inside a multiline comment.
            var mc = multilineCommentRegex.Matches(editor.Text,
                0);
            var mlcCount = mc.Count;
            for (var i = 0; i < mlcCount; ++i)
            {
                if (caretOffset >= mc[i].Index)
                {
                    if (caretOffset > mc[i].Index + mc[i].Length)
                    {
                        continue;
                    }

                    HideTooltip();
                    return;
                }

                break;
            }
            
            var showDoc = false;
            var showAC = false;
            
            if (lineOffset > 0)
            {
                showDoc = ComputeIntelliSense(text, lineOffset);
                showAC = ComputeAutoComplete(text, lineOffset, quotationCount);
            }

            if (!showDoc)
                HideDoc();
            
            if (!showAC)
                HideAC();
            
            if (!showDoc && !showAC)
                HideTooltip();
        }

        bool ComputeIntelliSense(string text, int lineOffset)
        {
            int xPos = int.MaxValue;

            #region IS

            var ISMatches = ISFindRegex.Matches(text);
            var scopeLevel = 0;
            for (var i = lineOffset - 1; i >= 0; --i)
            {
                if (text[i] == ')')
                {
                    scopeLevel++;
                }
                else if (text[i] == '(')
                {
                    scopeLevel--;
                    if (scopeLevel >= 0)
                    {
                        continue;
                    }

                    var foundMatch = false;
                    for (var j = 0; j < ISMatches.Count; ++j)
                    {
                        // Check that the cursor inside the match.
                        if (i < ISMatches[j].Index || i > ISMatches[j].Index + ISMatches[j].Length)
                        {
                            continue;
                        }

                        foundMatch = true;
                        var testString = ISMatches[j].Groups["name"].Value;
                        var classString = ISMatches[j].Groups["class"].Value;
                        var methodString = ISMatches[j].Groups["method"]?.Value;
                        if (classString.Length > 0)
                        {
                            var found = false;

                            // Match for static methods. Like MyClass.StaticMethod().
                            var staticMethodMap = methodMaps.FirstOrDefault(e => e.Name == classString);
                            var staticMethod =
                                staticMethodMap?.Methods.FirstOrDefault(e => e.Name == methodString);
                            if (staticMethod != null)
                            {
                                xPos = ISMatches[j].Groups["method"].Index +
                                       ISMatches[j].Groups["method"].Length;

                                ShowTooltip(xPos, staticMethod.FullName, staticMethod.CommentString);
                                // return;
                            }

                            // Try to find declaration
                            if (!found)
                            {
                                var pattern =
                                    $@"\b((?<class>[a-zA-Z_]([a-zA-Z0-9_]?)+))\s+({classString})\s*(;|=)";
                                var findDecl = new Regex(pattern, RegexOptions.Compiled);
                                var match = findDecl.Match(editor.Text);
                                var classMatch = match.Groups["class"].Value;
                                if (classMatch.Length > 0)
                                {
                                    var methodMap = methodMaps.FirstOrDefault(e => e.Name == classMatch);
                                    var method =
                                        methodMap?.Methods.FirstOrDefault(e => e.Name == methodString);
                                    if (method != null)
                                    {
                                        xPos = ISMatches[j].Groups["method"].Index +
                                               ISMatches[j].Groups["method"].Length;
                                        ShowTooltip(xPos, method.FullName, method.CommentString);
                                        // return;
                                    }
                                }
                            }

                            // Match the first found
                            if (!found)
                            {
                                // Match any methodmap, since the ide is not aware of the types
                                foreach (var methodMap in methodMaps)
                                {
                                    var method =
                                        methodMap.Methods.FirstOrDefault(e => e.Name == methodString);

                                    if (method == null)
                                    {
                                        continue;
                                    }

                                    xPos = ISMatches[j].Groups["method"].Index +
                                           ISMatches[j].Groups["method"].Length;
                                    ShowTooltip(xPos, method.FullName, method.CommentString);
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            var func = _func.FirstOrDefault(e => e.Name == testString);
                            if (func != null)
                            {
                                xPos = ISMatches[j].Groups["name"].Index +
                                       ISMatches[j].Groups["name"].Length;
                                ShowTooltip(xPos, func.FullName, func.CommentString);
                                return true;
                            }
                        }

                        break;
                    }

                    if (!foundMatch)
                    {
                        continue;
                    }

                    // ReSharper disable once RedundantAssignment
                    scopeLevel--; //i have no idea why this works...
                    break;
                }
            }

            #endregion

            return false;
        }

        bool ComputeAutoComplete(string text, int lineOffset, int quoteCount)
        {
            var acType = ACType.Toplevel;
            var xPos = int.MaxValue;

            //TODO: Check for multi-line strings.
            // Auto-complete for preprocessor statements.
            var defMatch = PreprocessorRegex.Match(text);
            var matchIndex = defMatch.Index;
            var matchLen = defMatch.Length;
            if (text.Trim() == "#" || (text.Trim().StartsWith("#") && matchIndex + matchLen >= lineOffset &&
                                       lineOffset > matchIndex))
            {
                // Get only the preprocessor statement and not the full line 
                var endIndex = text.IndexOf(" ", StringComparison.Ordinal);
                var statement = text.Substring(1);
                if (endIndex != -1)
                {
                    statement = text.Substring(1, endIndex).ToLower();
                }

                // If the pro-processor is found close the dialog.
                if (statement.Length != 0 && PreProcList.Contains(statement.Trim()))
                {
                    HideAC();
                }
                else
                {
                    // Try to find a stmt that starts with the text
                    var selectedIndex = PreProcList.FindIndex(e => e.StartsWith(statement));

                    if (selectedIndex == -1)
                    {
                        // Find a stmt that contains the text
                        selectedIndex = PreProcList.FindIndex(e => e.Contains(statement));
                        if (selectedIndex == -1)
                        {
                            selectedIndex = 0;
                        }
                    }


                    PreProcAutocompleteBox.SelectedIndex = selectedIndex;
                    PreProcAutocompleteBox.ScrollIntoView(PreProcAutocompleteBox.SelectedItem);


                    ShowTooltip(xPos, ACType.PreProc);
                    return true;
                }
            }
            
            if (IsValidFunctionChar(text[lineOffset - 1]) && quoteCount % 2 == 0)
            {
                var isNextCharValid = true;
                if (text.Length > lineOffset)
                {
                    if (IsValidFunctionChar(text[lineOffset]) || text[lineOffset] == '(')
                    {
                        isNextCharValid = false;
                    }
                }

                if (isNextCharValid)
                {
                    var endOffset = lineOffset - 1;
                    for (var i = endOffset; i >= 0; --i)
                    {
                        if (!IsValidFunctionChar(text[i]))
                        {
                            if (text[i] == '.')
                            {
                                acType = ACType.Class;
                            }

                            break;
                        }

                        endOffset = i;
                    }

                    var testString = text.Substring(endOffset, lineOffset - 1 - endOffset + 1);
                    if (testString.Length > 0)
                    {
                        if (acType == ACType.Class)
                        {
                            for (var i = 0; i < _docEntries.Count; ++i)
                            {
                                if (!_docEntries[i].EntryName.StartsWith(testString,
                                        StringComparison.InvariantCultureIgnoreCase) ||
                                    testString == _docEntries[i].EntryName)
                                {
                                    continue;
                                }


                                MethodAutoCompleteBox.SelectedIndex = i;
                                MethodAutoCompleteBox.ScrollIntoView(MethodAutoCompleteBox.SelectedItem);
                                ShowTooltip(xPos, ACType.Class);
                                return true;
                            }
                        }
                        else
                        {
                            for (var i = 0; i < _acEntries.Count; ++i)
                            {
                                if (!_acEntries[i].EntryName.StartsWith(testString,
                                        StringComparison.InvariantCultureIgnoreCase) ||
                                    testString == _acEntries[i].EntryName)
                                {
                                    continue;
                                }

                                AutoCompleteBox.SelectedIndex = i;
                                AutoCompleteBox.ScrollIntoView(AutoCompleteBox.SelectedItem);
                                ShowTooltip(xPos, ACType.Toplevel);
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// Show the object documentation tooltip.
        void ShowTooltip(int xPos, string name, string desc)
        {
            ShowDoc(name, desc);
            ShowTooltip(xPos);
        }


        /// Show the autocomplete tooltip
        void ShowTooltip(int xPos, ACType acType)
        {
            _acType = acType;
            ShowAC(acType);
            ShowTooltip(xPos);
        }


        private bool ISAC_EvaluateKeyDownEvent(Key k)
        {
            if (!_isTooltipOpen || !_isAcOpen)
            {
                return false;
            }

            switch (k)
            {
                case Key.Enter:
                case Key.Tab:
                {
                    var tabToAutoc = Program.OptionsObject.Editor_TabToAutocomplete;
                    if ((k == Key.Tab && !tabToAutoc) || (k == Key.Enter && tabToAutoc))
                    {
                        return false;
                    }

                    var startOffset = editor.CaretOffset - 1;
                    var endOffset = startOffset;
                    for (var i = startOffset; i >= 0; --i)
                    {
                        if (!IsValidFunctionChar(editor.Document.GetCharAt(i)))
                        {
                            break;
                        }

                        endOffset = i;
                    }

                    var length = startOffset - endOffset;
                    string replaceString;
                    var setCaret = false;

                    switch (_acType)
                    {
                        case ACType.Toplevel:
                            replaceString = ((ACNode)AutoCompleteBox.SelectedItem).EntryName;
                            if (_acEntries[AutoCompleteBox.SelectedIndex].IsExecutable)
                            {
                                replaceString += "(" + (Program.OptionsObject.Editor_AutoCloseBrackets ? ")" : "");
                                setCaret = true;
                            }

                            break;
                        case ACType.Class:
                            replaceString = ((ISNode)MethodAutoCompleteBox.SelectedItem).EntryName;
                            if (_docEntries[MethodAutoCompleteBox.SelectedIndex].IsExecuteable)
                            {
                                replaceString += "(" + (Program.OptionsObject.Editor_AutoCloseBrackets ? ")" : "");
                                setCaret = true;
                            }

                            break;
                        case ACType.PreProc:
                            replaceString = ((ACNode)PreProcAutocompleteBox.SelectedItem).EntryName;
                            replaceString += " ";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    editor.Document.Replace(endOffset, length + 1, replaceString);
                    if (setCaret)
                    {
                        editor.CaretOffset--;
                    }

                    return true;
                }
                case Key.Up:
                {
                    ScrollBox(GetListBox(), 1);
                    return true;
                }
                case Key.Down:
                {
                    ScrollBox(GetListBox(), -1);
                    return true;
                }
                case Key.Escape:
                {
                    HideTooltip();
                    return true;
                }
                default:
                {
                    return false;
                }
            }
        }

        // Scroll the ListBox UP amount times.
        static private void ScrollBox(ListBox box, int amount)
        {
            amount = box.SelectedIndex - amount; // Minus since we need to invert the amount.
            box.SelectedIndex =
                Math.Max(0, Math.Min(box.Items.Count - 1, amount)); // Clamp the value between 0 and the items count.
            box.ScrollIntoView(box.SelectedItem);
        }

        // Get the current list box from the current _acType.
        private ListBox GetListBox()
        {
            return _acType switch
            {
                ACType.Toplevel => AutoCompleteBox,
                ACType.Class => MethodAutoCompleteBox,
                ACType.PreProc => PreProcAutocompleteBox,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void ShowTooltip(int forcedXPos)
        {
            SetTooltipPosition(forcedXPos);

            if (_isTooltipOpen)
            {
                return;
            }

            _isTooltipOpen = true;
            TooltipGrid.Visibility = Visibility.Visible;
            if (Program.OptionsObject.UI_Animations)
            {
                // Clicking inside function 
                _fadeTooltipIn.Begin();
            }
            else
            {
                TooltipGrid.Opacity = 1.0;
            }
        }

        private void HideTooltip()
        {
            if (!_isTooltipOpen)
            {
                return;
            }

            _isTooltipOpen = false;
            
            if (Program.OptionsObject.UI_Animations)
            {
                _fadeTooltipOut.Begin();
            }
            else
            {
                TooltipGrid.Opacity = 0.0;
                TooltipGrid.Visibility = Visibility.Collapsed;
            }
        }

        enum ACType
        {
            Toplevel,
            Class,
            PreProc
        }

        private void ShowAC(ACType acType)
        {
            if (!_isAcOpen)
            {
                _isAcOpen = true;
                ACBorder.Height = 175.0;
                if (Program.OptionsObject.UI_Animations)
                {
                    // Render the Autocomplete box
                    _fadeACIn.Begin();
                }
                else
                {
                    AutoCompleteBox.Width = 260.0;
                    MethodAutoCompleteBox.Width = 260.0;
                    PreProcAutocompleteBox.Width = 260.0;
                }
            }

            switch (acType)
            {
                case ACType.Toplevel:
                    if (Program.OptionsObject.UI_Animations)
                    {
                        _fadeFuncACIn.Begin();
                    }
                    else
                    {
                        AutoCompleteBox.Opacity = 1.0;
                        MethodAutoCompleteBox.Opacity = 0.0;
                        PreProcAutocompleteBox.Opacity = 0.0;
                    }

                    break;
                case ACType.Class:
                    if (Program.OptionsObject.UI_Animations)
                    {
                        // When show the autocomplete for an object like: hello.<something>.
                        _fadeMethodACIn.Begin();
                    }
                    else
                    {
                        MethodAutoCompleteBox.Opacity = 1.0;
                        AutoCompleteBox.Opacity = 0.0;
                        PreProcAutocompleteBox.Opacity = 0.0;
                    }

                    break;
                case ACType.PreProc:
                    if (Program.OptionsObject.UI_Animations)
                    {
                        // When show the autocomplete for an object like: hello.<something>.
                        _fadePreProcACIn.Begin();
                    }
                    else
                    {
                        PreProcAutocompleteBox.Opacity = 1.0;
                        MethodAutoCompleteBox.Opacity = 0.0;
                        AutoCompleteBox.Opacity = 0.0;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(acType), acType, null);
            }
        }

        private void HideAC()
        {
            if (_isAcOpen)
            {
                _isAcOpen = false;
                if (Program.OptionsObject.UI_Animations)
                {
                    _fadeACOut.Begin();
                }
                else
                {
                    AutoCompleteBox.Width = 0.0;
                    MethodAutoCompleteBox.Width = 0.0;
                    PreProcAutocompleteBox.Width = 0.0;
                    ACBorder.Height = 0.0;
                }
            }
        }

        private void ShowDoc(string signature, string documentation)
        {
            if (!_isDocOpen)
            {
                _isDocOpen = true;
                DocColumn.Width = new GridLength(0.0, GridUnitType.Auto);
            }

            DocFuncSignature.Text = signature;
            DocFuncDescription.Text = documentation;
        }

        private void HideDoc()
        {
            if (_isDocOpen)
            {
                _isDocOpen = false;
                DocColumn.Width = new GridLength(0.0);
            }

            DocFuncDescription.Text = string.Empty;
            DocFuncSignature.Text = string.Empty;
        }

        private void SetTooltipPosition(int forcedXPos = int.MaxValue)
        {
            Point p;
            if (forcedXPos != int.MaxValue)
            {
                var tvp = new TextViewPosition(editor.TextArea.Caret.Position.Line, forcedXPos + 1);
                p = editor.TextArea.TextView.GetVisualPosition(tvp, VisualYPosition.LineBottom) -
                    editor.TextArea.TextView.ScrollOffset;
            }
            else
            {
                p = editor.TextArea.TextView.GetVisualPosition(editor.TextArea.Caret.Position,
                    VisualYPosition.LineBottom) - editor.TextArea.TextView.ScrollOffset;
            }

            DocFuncDescription.Measure(new Size(double.MaxValue, double.MaxValue));
            var y = p.Y;
            var ISACHeight = 0.0;
            if (_isAcOpen && _isDocOpen)
            {
                var ISHeight = DocFuncDescription.DesiredSize.Height;
                ISACHeight = Math.Max(175.0, ISHeight);
            }
            else if (_isAcOpen)
            {
                ISACHeight = 175.0;
            }
            else if (_isDocOpen)
            {
                ISACHeight = DocFuncDescription.DesiredSize.Height;
            }

            if (y + ISACHeight > editor.ActualHeight)
            {
                y = (editor.TextArea.TextView.GetVisualPosition(editor.TextArea.Caret.Position,
                    VisualYPosition.LineTop) - editor.TextArea.TextView.ScrollOffset).Y;
                y -= ISACHeight;
            }

            TooltipGrid.Margin =
                new Thickness(p.X + ((LineNumberMargin)editor.TextArea.LeftMargins[0]).ActualWidth + 20.0, y, 0.0,
                    0.0);
        }

        private void FadeTooltipOutCompleted(object sender, EventArgs e)
        {
            // TooltipGrid.Visibility = Visibility.Collapsed;
        }

        private void FadeACOut_Completed(object sender, EventArgs e)
        {
            if (_fadeACIn.GetCurrentState() != ClockState.Active)
            {
                ACBorder.Height = 0.0;
            }
        }

        // Check if char is between a-z A-Z 0-9 or _.
        private static bool IsValidFunctionChar(char c)
        {
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_')
            {
                return true;
            }

            return false;
        }
    }
}