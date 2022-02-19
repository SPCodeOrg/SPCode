using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using SourcepawnCondenser.SourcemodDefinition;

// ReSharper disable once CheckNamespace
namespace SPCode.UI.Components
{
    enum ACType
    {
        Toplevel,
        Class,
        PreProc
    }

    /// @note
    // AC stands for AutoComplete
    // IS stands for IntelliSense and is often replaced with "Doc"/"Documentation"
    //
    // _smDef is the SMDefinition that contains all the symbols of the include directory and open file,
    // currentSmDef contains only the current file symbols.
    public partial class EditorElement
    {
        private bool _isAcOpen;
        private List<ACNode> _acEntries;

        private readonly List<ISNode> _methodACEntries = new();

        private bool _isDocOpen;

        private bool _isTooltipOpen;


        private bool _animationsLoaded;

        private Storyboard _fadeFuncACIn;
        private Storyboard _fadeMethodACIn;
        private Storyboard _fadePreProcACIn;

        private Storyboard _fadeACIn;
        private Storyboard _fadeACOut;
        private Storyboard _fadeTooltipIn;
        private Storyboard _fadeTooltipOut;


        /// Used to keep track of the current autocomplete type (ie. toplevel, class or preprocessor)
        private ACType _acType = ACType.Toplevel;

        private SMDefinition _smDef;

        /// Matches either a function call ("PrintToChat(...)") or a method call ("arrayList.Push(...)")
        private readonly Regex _isFindRegex = new(
            @"\b(((?<class>[a-zA-Z_]([a-zA-Z0-9_]?)+)\.)?(?<method>[a-zA-Z_]([a-zA-Z0-9_]?)+)\()",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        // TODO Add EnumStructs

        private readonly Regex _multilineCommentRegex = new(@"/\*.*?\*/",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        // Pre-processor statements
        static private readonly string[] PreProcArr =
        {
            "assert", "define", "else", "elseif", "endif", "endinput", "endscript", "error", "warning", "if",
            "include", "line", "pragma", "tryinclude", "undef"
        };

        static private readonly List<string> PreProcList = PreProcArr.ToList();

        static private readonly IEnumerable<ACNode>
            PreProcNodes = ACNode.ConvertFromStringList(PreProcList, false, "#", true);


        static private readonly Regex PreprocessorRegex = new("#\\w+", RegexOptions.Compiled);

        /// <summary>
        ///  This is called only one time when the program first opens.
        /// </summary>
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

                // _fadeTooltipOut.Completed += FadeTooltipOutCompleted;
                _fadeACOut.Completed += FadeACOut_Completed;

                _animationsLoaded = true;
            }

            if (_isTooltipOpen)
            {
                HideTooltip();
            }

            _smDef = Program.Configs[Program.SelectedConfig].GetSMDef();
            _acEntries = _smDef.ProduceACNodes();


            AutoCompleteBox.ItemsSource = _acEntries;
            MethodAutoCompleteBox.ItemsSource = _methodACEntries;
            PreProcAutocompleteBox.ItemsSource = ACNode.ConvertFromStringList(PreProcList, false, "#", true);
        }

        /// <summary>
        ///  This is called several times. Mostly when the caret position changes.
        /// </summary>
        /// <param name="smDef"> The SMDefinition </param>
        private void InterruptLoadAutoCompletes(SMDefinition smDef)
        {
            var acNodes = smDef.ProduceACNodes();
            Dispatcher?.Invoke(() =>
            {
                _acEntries = acNodes;
                AutoCompleteBox.ItemsSource = _acEntries;
                PreProcAutocompleteBox.ItemsSource = PreProcNodes;
                _smDef = smDef;
            });
        }

        private void EvaluateIntelliSense()
        {
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
            var mc = _multilineCommentRegex.Matches(editor.Text,
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

            // If the Doc or the Autocomplete is not shown we need to hide them.
            if (!showDoc)
                HideDoc();

            if (!showAC)
                HideAC();

            if (!showDoc && !showAC)
                HideTooltip();
        }

        /// <summary>
        /// Triggers the Documentation tooltip to be shown if a matching symbol is found.
        /// </summary>
        /// <returns>True if a the IntelliSense matched a symbol false otherwise</returns>
        bool ComputeIntelliSense(string text, int lineOffset)
        {
            //TODO: Add support for EnumStructs
            var isMatches = _isFindRegex.Matches(text);
            var scopeLevel = 0;

            // Iterate the line characters in reverse starting from the caret position.
            for (var i = lineOffset - 1; i >= 0; --i)
            {
                if (text[i] == ')')
                {
                    scopeLevel++;
                }
                else if (text[i] == '(')
                {
                    scopeLevel--;

                    // Check that we are inside a scope (eg. "PrintToChat(--HERE--)" )
                    if (scopeLevel >= 0)
                    {
                        continue;
                    }

                    for (var j = 0; j < isMatches.Count; ++j)
                    {
                        // Check that the cursor inside the match.
                        if (i < isMatches[j].Index || i > isMatches[j].Index + isMatches[j].Length)
                        {
                            continue;
                        }

                        var classString = isMatches[j].Groups["class"].Value;
                        var methodString =
                            isMatches[j].Groups["method"]
                                .Value; // If we are not inside a method call this is a classic function call (eg. "PrintToChat(...)")

                        var xPos = isMatches[j].Groups["method"].Index +
                                   isMatches[j].Groups["method"].Length;

                        if (classString.Length > 0)
                        {
                            var methodMap = FindMethodMap(classString);

                            var method = methodMap?.Methods.FirstOrDefault(e => e.Name == methodString);

                            if (method == null)
                                continue;

                            ShowTooltip(xPos, method.FullName, method.CommentString);
                            return true;
                        }

                        // Try to find the function definition.
                        var func = _smDef.Functions.FirstOrDefault(e => e.Name == methodString);
                        if (func == null)
                            continue;

                        ShowTooltip(xPos, func.FullName, func.CommentString);
                        return true;
                    }

                    break;
                }
            }

            return false;
        }


        SMMethodmap? FindMethodMap(string classStr)
        {
            // Match for static methods. Like MyClass.StaticMethod(). Look for a MethodMap that is named as our classStr.
            var methodMap = _smDef.Methodmaps.FirstOrDefault(e => e.Name == classStr);

            // If the staticMethod is found show it.
            if (methodMap != null)
            {
                return methodMap;
            }

            // Find variable declaration to see of what type it is -->
            // Try to match it in the local variables (of the current function).
            var varDecl =
                _smDef?.CurrentFunction?.FuncVariables.FirstOrDefault(e => e.Name == classStr);

            //TODO: Add FunctionParameters matching.
            /*varDecl ??= _smDef?.CurrentFunction?.Parameters.FirstOrDefault(e =>
                MatchTypeRegex.Match(e).Groups[0].Value == classString);*/

            // Try to match it in the current file.
            varDecl ??= _smDef.Variables.FirstOrDefault(e => e.Name == classStr);

            return varDecl == null ? null : _smDef.Methodmaps.FirstOrDefault(e => e.Name == varDecl.Type);

            // If we found the declaration get the Variable Type and look for a methodmap matching its type.
        }

        /// <summary>
        /// Triggers the AutoComplete tooltip to be shown if a suggestion is found.
        /// </summary>
        /// <returns>True if a the AutoComplete matched a symbol false otherwise</returns>
        bool ComputeAutoComplete(string text, int lineOffset, int quoteCount)
        {
            // Return if we are inside a string.
            if (quoteCount % 2 != 0)
                return false;

            var acType = ACType.Toplevel;
            const int xPos = int.MaxValue;

            //TODO: Check for multi-line strings. (Actually currently the Program doesn't support the declarations split between two lines).

            /*** Auto-complete for preprocessor statements. ***/
            var defMatch = PreprocessorRegex.Match(text);
            var matchIndex = defMatch.Index;
            var matchLen = defMatch.Length;

            // Check that we are inside a preprocessor statement and that the caret is on the definition, eg. "#<here>" and not "#define <here>".
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

                // If the preprocessor stmt found close the dialog.
                if (statement.Length != 0 && PreProcList.Contains(statement.Trim()))
                {
                    HideAC();
                    return true;
                }

                // Try to find a stmt that starts with the text
                var selectedIndex = PreProcList.FindIndex(e => e.StartsWith(statement));

                if (selectedIndex == -1)
                {
                    // If no stmt starts with the text find one try to find one that contains it.
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


            if (!IsValidFunctionChar(text[lineOffset - 1]))
                return false;

            var isNextCharValid = true;
            if (text.Length > lineOffset)
            {
                if (IsValidFunctionChar(text[lineOffset]) || text[lineOffset] == '(')
                {
                    isNextCharValid = false;
                }
            }

            if (!isNextCharValid)
            {
                return false;
            }


            // Check if we are calling a Method.
            var dotOffset = lineOffset - 1;

            // Try to find the "." index from the caret position.
            for (var i = dotOffset; i >= 0; --i)
            {
                if (!IsValidFunctionChar(text[i]))
                {
                    if (text[i] == '.')
                    {
                        acType = ACType.Class;
                    }

                    break;
                }

                // Save the "." index.
                dotOffset = i;
            }

            var methodString = text.Substring(dotOffset, lineOffset - 1 - dotOffset + 1);

            if (methodString.Length <= 0)
            {
                return false;
            }

            int classOffset = dotOffset - 2;

            if (acType == ACType.Class)
            {
                int len = 0;
                for (var i = classOffset; i >= 0; --i)
                {
                    if (!IsValidFunctionChar(text[i]))
                    {
                        break;
                    }

                    // Save the index where the method call begins, eg. ArrayList.Push().
                    //                                                  ^HERE
                    classOffset = i;
                    len++;
                }

                var classString = text.Substring(classOffset, len);
                var mm = FindMethodMap(classString);
                if (mm == null)
                {
                    return false;
                }

                var isNodes = mm.ProduceISNodes();

                _methodACEntries.Clear();
                _methodACEntries.AddRange(isNodes);

                for (var i = 0; i < isNodes.Count; i++)
                {
                    var node = isNodes[i];

                    if (node.EntryName == methodString)
                        return false;

                    if (!node.EntryName.StartsWith(methodString))
                        continue;

                    // We need to hide the doc (and then re-show it) to properly update the list.
                    HideDoc();

                    MethodAutoCompleteBox.SelectedIndex = i;
                    MethodAutoCompleteBox.ScrollIntoView(MethodAutoCompleteBox.SelectedItem);
                    ShowTooltip(xPos, ACType.Class);
                    return true;
                }

                return false;
            }

            for (var i = 0; i < _acEntries.Count; ++i)
            {
                if (!_acEntries[i].EntryName.StartsWith(methodString,
                        StringComparison.InvariantCultureIgnoreCase) ||
                    methodString == _acEntries[i].EntryName)
                {
                    continue;
                }

                AutoCompleteBox.SelectedIndex = i;
                AutoCompleteBox.ScrollIntoView(AutoCompleteBox.SelectedItem);
                ShowTooltip(xPos, ACType.Toplevel);
                return true;
            }


            return false;
        }

        private bool ISAC_EvaluateKeyDownEvent(Key k)
        {
            if (!_isTooltipOpen)
            {
                return false;
            }

            if (k == Key.Escape)
            {
                HideTooltip();
                return true;
            }

            if (!_isAcOpen)
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
                            if (_methodACEntries[MethodAutoCompleteBox.SelectedIndex].IsExecuteable)
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
                default:
                {
                    return false;
                }
            }
        }

        /// Scroll the ListBox UP amount times.
        static private void ScrollBox(ListBox box, int amount)
        {
            amount = box.SelectedIndex - amount; // Minus since we need to invert the amount.
            box.SelectedIndex =
                Math.Max(0, Math.Min(box.Items.Count - 1, amount)); // Clamp the value between 0 and the items count.
            box.ScrollIntoView(box.SelectedItem);
        }

        /// Get the current list box from the current _acType.
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

        /// <summary>
        /// Actually shows the tooltip.
        /// Should only be called using <see cref="ShowTooltip(int, string, string)"/> or <see cref="ShowTooltip(int, ACType)"/>
        /// </summary>
        /// <param name="forcedXPos"></param>
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

        /// <summary>
        /// Shows the Documentation tooltip.
        /// </summary>
        /// <param name="xPos">Tooltip position.</param>
        /// <param name="name">The first line. Usually the function signature</param>
        /// <param name="desc">The function documentation.</param>
        void ShowTooltip(int xPos, string name, string desc)
        {
            ShowDoc(name, desc);
            ShowTooltip(xPos);
        }


        /// <summary>
        /// Shows the Autocomplete tooltip.
        /// </summary>
        /// <param name="xPos">Tooltip position</param>
        /// <param name="acType">Autocomplete type</param>
        void ShowTooltip(int xPos, ACType acType)
        {
            _acType = acType;
            ShowAC(acType);
            ShowTooltip(xPos);
        }

        /// <summary>
        /// Shows the Autocomplete tooltip.
        /// This should be shown with <see cref="ShowTooltip(int, ACType)"/>
        /// </summary>
        /// <param name="acType">The autocomplete type</param>
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

        /// <summary>
        /// Shows the Documentation tooltip.
        /// This should be shown with <see cref="ShowTooltip(int,string,string)"/>
        /// </summary>
        /// <param name="signature">The first line. Usually the function signature</param>
        /// <param name="documentation">The function documentation.</param>
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

        /// <summary>
        /// Hides the tooltip
        /// </summary>
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

        /// <summary>
        /// Hides the AutoComplete tooltip
        /// </summary>
        private void HideAC()
        {
            if (!_isAcOpen)
            {
                return;
            }

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

        /// <summary>
        /// Hides the Documentation(IS) tooltip
        /// </summary>
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

        /// <summary>
        /// Sets the tooltip position.
        /// This is done automatically in ShowTooltip.
        /// </summary>
        /// <param name="forcedXPos"></param>
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
            var tooltipHeight = 0.0;

            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (_isAcOpen && _isDocOpen)
            {
                var isHeight = DocFuncDescription.DesiredSize.Height;
                tooltipHeight = Math.Max(175.0, isHeight);
            }
            else if (_isAcOpen)
            {
                tooltipHeight = 175.0;
            }
            else if (_isDocOpen)
            {
                tooltipHeight = DocFuncDescription.DesiredSize.Height;
            }

            if (y + tooltipHeight > editor.ActualHeight)
            {
                y = (editor.TextArea.TextView.GetVisualPosition(editor.TextArea.Caret.Position,
                    VisualYPosition.LineTop) - editor.TextArea.TextView.ScrollOffset).Y;
                y -= tooltipHeight;
            }

            TooltipGrid.Margin =
                new Thickness(p.X + ((LineNumberMargin)editor.TextArea.LeftMargins[0]).ActualWidth + 20.0, y, 0.0,
                    0.0);
        }

        private void FadeACOut_Completed(object sender, EventArgs e)
        {
            if (_fadeACIn.GetCurrentState() != ClockState.Active)
            {
                ACBorder.Height = 0.0;
            }
        }

        // Check if char is between a-z A-Z 0-9 or _.
        private static bool IsValidFunctionChar(char c) =>
            (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
    }
}