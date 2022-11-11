using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using SourcepawnCondenser;
using SourcepawnCondenser.SourcemodDefinition;
using SPCode.Interop;

// ReSharper disable once CheckNamespace
namespace SPCode.UI.Components;

enum ACType
{
    /// <summary>
    /// Top level objects, such as Functions, Variables, Types.
    /// </summary> 
    Toplevel,

    /// <summary>
    /// Class Methods and Fields. Also used for MethodMap list.
    /// </summary> 
    Class,

    /// <summary>
    /// Pre-processor statements.
    /// </summary> 
    PreProc,
}

// AC stands for AutoComplete
// IS stands for IntelliSense and is often replaced with "Doc"/"Documentation"
//
// _smDef is the SMDefinition that contains all the symbols of the include directory and open file,
// currentSmDef contains only the current file symbols.

public partial class EditorElement
{
    private bool _isAcOpen;
    private List<ACNode> _acEntries;

    /// <summary>
    /// We use this just to keep track of the current isNodes for the equality check. <br></br>
    /// Seems like that using this as ItemsSource for the MethodAutoCompleteBox causes it to not update the UI <br></br>
    /// when ScrollIntoView is called.
    /// </summary>
    private readonly List<ACNode> _methodACEntries = new();

    private bool _isDocOpen;

    private bool _isTooltipOpen;
    private bool _keepTooltipClosed;

    private bool _animationsLoaded;

    private Storyboard _fadeFuncACIn;
    private Storyboard _fadeMethodACIn;
    private Storyboard _fadePreProcACIn;

    private Storyboard _fadeACIn;
    private Storyboard _fadeACOut;
    private Storyboard _fadeTooltipIn;
    private Storyboard _fadeTooltipOut;

    private static readonly SMDefinition.ISNodeEqualityComparer ISEqualityComparer = new();

    /// <summary>
    /// Used to keep track of the current autocomplete type (ie. toplevel, class or preprocessor)
    /// </summary> 
    private ACType _acType = ACType.Toplevel;

    private SMDefinition _smDef;

    /// <summary>
    /// Matches either a function call ("PrintToChat(...)") or a method call ("arrayList.Push(...)")
    /// </summary> 
    private static readonly Regex ISFindRegex = new(
        @"\b(((?<class>[a-zA-Z_]([a-zA-Z0-9_]?)+)\.)?(?<method>[a-zA-Z_]([a-zA-Z0-9_]?)+)\()",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    private static readonly Regex NewRegex = new(@"(?:(\w+)\s+\w+\s+=\s+)?new\s+(\w+)?$", RegexOptions.Compiled);

    private static readonly Regex MultilineCommentRegex = new(@"/\*.*?\*/",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline);

    /// <summary>
    /// Pre-processor statements
    /// </summary> 
    private static readonly string[] PreProcArr =
    {
        "assert", "define", "else", "elseif", "endif", "endinput", "endscript", "error", "warning", "if",
        "include", "line", "pragma", "tryinclude", "undef"
    };

    private static readonly List<string> PreProcList = PreProcArr.ToList();

    private static readonly IEnumerable<ACNode>
        PreProcNodes = ACNode.ConvertFromStringList(PreProcList, false, "#", true);

    private static readonly Regex PreprocessorRegex = new("#\\w+", RegexOptions.Compiled);

    /// <summary>
    /// This is called only one time when the program first opens.
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
        _acEntries = new List<ACNode>();
        _acEntries.Clear();
        _acEntries = _smDef.ProduceACNodes();


        AutoCompleteBox.ItemsSource = _acEntries;
        PreProcAutocompleteBox.ItemsSource = ACNode.ConvertFromStringList(PreProcList, false, "#", true);
    }

    /// <summary>
    /// This is called several times. Mostly when the caret position changes.
    /// </summary>
    /// <param name="smDef"> The SMDefinition </param>
    private void InterruptLoadAutoCompletes(SMDefinition smDef)
    {
        _acEntries.Clear();
        _acEntries = smDef.ProduceACNodes();
        Dispatcher?.Invoke(() =>
        {
            //_acEntries = _acEntries;
            AutoCompleteBox.ItemsSource = _acEntries;
            PreProcAutocompleteBox.ItemsSource = PreProcNodes;
            _smDef = smDef;
        });
    }

    private int _lastLine = -1;

    private void EvaluateIntelliSense()
    {
        // Check if ESC was pressed and we still are in the same line.
        var currentLineIndex = editor.TextArea.Caret.Line - 1;

        if (_lastLine != currentLineIndex)
        {
            _keepTooltipClosed = false;
        }

        _lastLine = currentLineIndex;

        if (editor.SelectionLength > 0 || _keepTooltipClosed)
        {
            HideTooltip();
            return;
        }

        var line = editor.Document.Lines[currentLineIndex];
        var lineOffset = editor.TextArea.Caret.Column - 1;
        var text = editor.Document.GetText(line.Offset, line.Length);
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
        var mc = MultilineCommentRegex.Matches(editor.Text,
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
    private bool ComputeIntelliSense(string text, int lineOffset)
    {
        try
        {
            var isMatches = ISFindRegex.Matches(text);
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
                            var classObj = FindClass(classString);

                            SMObjectMethod? method = null;

                            switch (classObj)
                            {
                                case SMEnumStruct:
                                    method = classObj?.Methods.Find(e => e.Name == methodString);
                                    break;
                                case SMMethodmap obj:
                                    method = FindMethod(methodString, obj, _smDef);
                                    break;
                            }


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
        catch (Exception ex)
        {
            LoggingControl.LogAction($"Exception caught in {ex.TargetSite}: {ex.Message}");
            return false;
        }
    }

    private SMClasslike? FindClass(string classStr)
    {
        // Match for static methods. Like MyClass.StaticMethod(). Look for a MethodMap that is named as our classStr.
        var classElement = _smDef.Methodmaps.FirstOrDefault(e => e.Name == classStr);

        // If the staticMethod is found show it.
        if (classElement != null)
        {
            return classElement;
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

        if (varDecl == null)
        {
            return null;
        }

        // If we found the declaration get the Variable Type and look for a method-map matching its type.

        return (SMClasslike)_smDef.Methodmaps.FirstOrDefault(e => e.Name == varDecl.Type) ??
               _smDef.EnumStructs.FirstOrDefault(e => e.Name == varDecl.Type);
    }

    private SMObjectMethod? FindMethod(string methodName, SMMethodmap methodMap, SMDefinition smDef)
    {
        var mm = methodMap;
        while (mm != null)
        {
            var method = mm.Methods.Find(e => e.Name == methodName);
            if (method != null)
                return method;

            if (mm.InheritedType.Length == 0)
                return null;

            mm = smDef.Methodmaps.Find(e => e.Name == mm.InheritedType);
        }

        return null;
    }

    /// <summary>
    /// Triggers the AutoComplete tooltip to be shown if a suggestion is found.
    /// </summary>
    /// <returns>True if a the AutoComplete matched a symbol false otherwise</returns>
    private bool ComputeAutoComplete(string text, int lineOffset, int quoteCount)
    {
        try
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

            var trimText = text.Trim();
            // Check that we are inside a preprocessor statement and that the caret is on the definition, eg. "#<here>" and not "#define <here>".
            if (trimText == "#" || (trimText.StartsWith("#") && matchIndex + matchLen >= lineOffset &&
                                    lineOffset > matchIndex))
            {
                // Get only the preprocessor statement and not the full line 
                string statement = trimText;
                if (statement != "#")
                {
                    var endIndex = trimText.IndexOf(" ", StringComparison.Ordinal);
                    if (endIndex == -1)
                        endIndex = trimText.Length;
                    statement = trimText.Substring(1, endIndex - 1);
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


            if (text.Length == 0 || editor.SelectionLength > 0)
                return false;

            if (!IsValidFunctionChar(text[lineOffset - 1]) &&
                text[lineOffset - 1] != '.' && text[lineOffset - 1] != ' ' && text[lineOffset - 1] != '\t')
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

            var methodString = text.Substring(dotOffset, lineOffset - dotOffset);
            if (methodString == ".")
            {
                dotOffset++;
            }

            if (methodString.Length <= 0)
            {
                return false;
            }

            int classOffset = dotOffset - 2;

            if (acType == ACType.Class)
            {
                if (classOffset < 0)
                {
                    return false;
                }

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
                var mm = FindClass(classString);
                if (mm == null)
                {
                    return false;
                }

                var isNodes = mm.ProduceNodes(_smDef);

                if (!isNodes.SequenceEqual(_methodACEntries, ISEqualityComparer))
                {
                    MethodAutoCompleteBox.Items.Clear();
                    isNodes.ForEach(e => MethodAutoCompleteBox.Items.Add(e));

                    _methodACEntries.Clear();
                    _methodACEntries.AddRange(isNodes);

                    MethodAutoCompleteBox.UpdateLayout();
                }

                if (methodString == ".")
                {
                    MethodAutoCompleteBox.SelectedIndex = 0;
                    MethodAutoCompleteBox.ScrollIntoView(MethodAutoCompleteBox.SelectedItem);

                    ShowTooltip(xPos, ACType.Class);
                    return true;
                }

                var index = isNodes.FindNode(methodString);

                if (index == null)
                    return false;

                MethodAutoCompleteBox.SelectedIndex = (int)index;
                MethodAutoCompleteBox.ScrollIntoView(MethodAutoCompleteBox.SelectedItem);

                ShowTooltip(xPos, ACType.Class);

                return true;
            }

            // Match MethodMap initializations.
            var match = NewRegex.Match(text.Substring(0, lineOffset));
            if (match.Success)
            {
                var isNodes = ACNode.ConvertFromStringList(_smDef.Methodmaps.Select(e => e.Name), true, "• ")
                    .ToList();

                if (!isNodes.SequenceEqual(_methodACEntries, ISEqualityComparer))
                {
                    MethodAutoCompleteBox.Items.Clear();
                    isNodes.ForEach(e => MethodAutoCompleteBox.Items.Add(e));

                    _methodACEntries.Clear();
                    _methodACEntries.AddRange(isNodes);

                    MethodAutoCompleteBox.UpdateLayout();
                }

                var methodMapName = match.Groups[2].Value;

                if (methodMapName == "")
                {
                    var boxIndex = 0;
                    var varType = match.Groups[1].Value;
                    if (varType != "")
                    {
                        var mmIndex = _methodACEntries.FindIndex(e => e.EntryName.StartsWith(varType));
                        if (mmIndex != -1)
                        {
                            boxIndex = mmIndex;
                        }
                    }

                    MethodAutoCompleteBox.SelectedIndex = boxIndex;
                    MethodAutoCompleteBox.ScrollIntoView(MethodAutoCompleteBox.SelectedItem);

                    ShowTooltip(xPos, ACType.Class);
                    return true;
                }

                var index = isNodes.FindNode(methodMapName);

                if (index == null)
                {
                    return false;
                }

                MethodAutoCompleteBox.SelectedIndex = (int)index;
                MethodAutoCompleteBox.ScrollIntoView(MethodAutoCompleteBox.SelectedItem);

                ShowTooltip(xPos, ACType.Class);

                return true;
            }

            // Match functions and other symbols like Variables, Types, ...
            var funcIndex =
                _acEntries.FindIndex(e => e.EntryName.StartsWith(methodString) && methodString != e.EntryName);
            if (funcIndex == -1)
            {
                // Re-try without case sensitivity.
                funcIndex = _acEntries.FindIndex(e =>
                    e.EntryName.StartsWith(methodString, StringComparison.InvariantCultureIgnoreCase) &&
                    methodString != e.EntryName);
            }

            // If not found
            if (funcIndex == -1)
            {
                return false;
            }

            AutoCompleteBox.SelectedIndex = funcIndex;
            AutoCompleteBox.ScrollIntoView(AutoCompleteBox.SelectedItem);
            ShowTooltip(xPos, ACType.Toplevel);
            return true;
        }
        catch (Exception ex)
        {
            LoggingControl.LogAction($"Exception caught in {ex.TargetSite}: {ex.Message}");
            return false;
        }
    }

    private bool ISAC_EvaluateKeyDownEvent(Key k)
    {
        try
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && k == Key.Space)
            {
                _keepTooltipClosed = false;
                var currentLineIndex = editor.TextArea.Caret.Line - 1;
                var line = editor.Document.Lines[currentLineIndex];
                var text = editor.Document.GetText(line.Offset, line.Length);
                var lineOffset = editor.TextArea.Caret.Column - 1;

                ComputeAutoComplete(text, lineOffset, 0);
                return true;
            }

            if (!_isTooltipOpen)
            {
                return false;
            }

            if (k == Key.Escape)
            {
                HideTooltip();
                _keepTooltipClosed = true;
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
                    var tabToAutoC = Program.OptionsObject.Editor_TabToAutocomplete;
                    if ((k == Key.Tab && !tabToAutoC) || (k == Key.Enter && tabToAutoC))
                    {
                        return false;
                    }

                    // HideTooltip();

                    var startOffset = editor.CaretOffset - 1;
                    var endOffset = startOffset;
                    for (var i = startOffset; i >= 0; --i)
                    {
                        var charAt = editor.Document.GetCharAt(i);
                        if (!IsValidFunctionChar(charAt))
                        {
                            if (i == startOffset && (charAt is '.' or ' ' or '\t' or '#'))
                            {
                                endOffset = i + 1;
                            }

                            break;
                        }

                        endOffset = i;
                    }

                    var length = startOffset - endOffset;
                    string replaceString;
                    var setCaret = 0;


                    var item = (ACNode)CurrentBox.SelectedItem;
                    switch (_acType)
                    {
                        case ACType.Toplevel:
                        case ACType.Class:
                            replaceString = item.EntryName;
                            if (item.IsExecutable)
                            {
                                replaceString += "(" + (Program.OptionsObject.Editor_AutoCloseBrackets ? ")" : "");
                                setCaret = -1;
                            }

                            break;

                        case ACType.PreProc:
                            replaceString = item.EntryName + " ";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    editor.Document.Replace(endOffset, length + 1, replaceString);
                    editor.CaretOffset += setCaret;

                    return true;
                }
                case Key.Up:
                {
                    ScrollBox(1);
                    return true;
                }
                case Key.Down:
                {
                    ScrollBox(-1);
                    return true;
                }
                default:
                {
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            LoggingControl.LogAction($"Exception caught in {ex.TargetSite}: {ex.Message}");
            throw;
        }
    }

    /// Scroll the ListBox UP amount times.
    private void ScrollBox(int amount, ListBox? box = null)
    {
        box ??= CurrentBox;
        amount = box.SelectedIndex - amount; // Minus since we need to invert the amount.
        box.SelectedIndex =
            Math.Max(0, Math.Min(box.Items.Count - 1, amount)); // Clamp the value between 0 and the items count.
        box.ScrollIntoView(box.SelectedItem);
    }

    /// Get the current list box from the current _acType.
    /// The MethodAutoCompleteBox is also used for MethodMaps "new statments".
    private ListBox CurrentBox
    {
        get
        {
            return _acType switch
            {
                ACType.Toplevel => AutoCompleteBox,
                ACType.Class => MethodAutoCompleteBox,
                ACType.PreProc => PreProcAutocompleteBox,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
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
        if (_isAcOpen)
        {
            HideAC();
        }

        if (_isDocOpen)
        {
            HideDoc();
        }

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

public static class ACNodeExt
{
    public static int? FindNode(this List<ACNode> nodes, string query)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];

            if (node.EntryName == query)
                return null;

            if (!node.EntryName.StartsWith(query))
                continue;

            return i;
        }

        var index = nodes.FindIndex(node =>
            node.EntryName.StartsWith(query, StringComparison.InvariantCultureIgnoreCase));
        if (index == -1)
        {
            index = nodes.FindIndex(node => node.EntryName.Contains(query));
        }

        return index == -1 ? null : index;
    }
}