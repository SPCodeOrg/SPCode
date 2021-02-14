using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using SourcepawnCondenser.SourcemodDefinition;

namespace SPCode.UI.Components
{
    public partial class EditorElement
    {
        private bool AC_IsFuncC = true;
        private bool AC_Open;
        private ACNode[] acEntrys;

        private bool AnimationsLoaded;

        private Storyboard FadeAC_FuncC_In;
        private Storyboard FadeAC_MethodC_In;

        private Storyboard FadeACIn;
        private Storyboard FadeACOut;
        private Storyboard FadeISACIn;
        private Storyboard FadeISACOut;

        private SMFunction[] funcs;
        private bool IS_Open;
        public bool ISAC_Open;
        private ISNode[] isEntrys;

        private readonly Regex ISFindRegex = new Regex(
            @"\b((?<class>[a-zA-Z_]([a-zA-Z0-9_]?)+)\.(?<method>[a-zA-Z_]([a-zA-Z0-9_]?)+)\()|((?<name>[a-zA-Z_]([a-zA-Z0-9_]?)+)\()",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        private int LastShowedLine = -1;

        // TODO Add EnumStructs
        private SMMethodmap[] methodMaps;

        private readonly Regex multilineCommentRegex = new Regex(@"/\*.*?\*/",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        //private string[] methodNames;
        public void LoadAutoCompletes()
        {
            if (!AnimationsLoaded)
            {
                FadeISACIn = (Storyboard)Resources["FadeISACIn"];
                FadeISACOut = (Storyboard)Resources["FadeISACOut"];
                FadeACIn = (Storyboard)Resources["FadeACIn"];
                FadeACOut = (Storyboard)Resources["FadeACOut"];
                FadeAC_FuncC_In = (Storyboard)Resources["FadeAC_FuncC_In"];
                FadeAC_MethodC_In = (Storyboard)Resources["FadeAC_MethodC_In"];
                FadeISACOut.Completed += FadeISACOut_Completed;
                FadeACOut.Completed += FadeACOut_Completed;
                AnimationsLoaded = true;
            }

            if (ISAC_Open)
            {
                HideISAC();
            }

            var def = Program.Configs[Program.SelectedConfig].GetSMDef();
            funcs = def.Functions.ToArray();
            acEntrys = def.ProduceACNodes();
            isEntrys = def.ProduceISNodes();
            methodMaps = def.Methodmaps.ToArray();
            AutoCompleteBox.ItemsSource = acEntrys;
            MethodAutoCompleteBox.ItemsSource = isEntrys;
        }

        public void InterruptLoadAutoCompletes(SMFunction[] FunctionArray, ACNode[] acNodes,
            ISNode[] isNodes, SMMethodmap[] newMethodMaps)
        {
            Dispatcher?.Invoke(() =>
            {
                funcs = FunctionArray;
                acEntrys = acNodes;
                isEntrys = isNodes;
                AutoCompleteBox.ItemsSource = acEntrys;
                MethodAutoCompleteBox.ItemsSource = isEntrys;
                methodMaps = newMethodMaps;
            });
        }

        //private readonly Regex methodExp = new Regex(@"(?<=\.)[A-Za-z_]\w*", RegexOptions.RightToLeft);
        private void EvaluateIntelliSense()
        {
            if (editor.SelectionLength > 0)
            {
                HideISAC();
                return;
            }

            var currentLineIndex = editor.TextArea.Caret.Line - 1;
            var line = editor.Document.Lines[currentLineIndex];
            var text = editor.Document.GetText(line.Offset, line.Length);
            var lineOffset = editor.TextArea.Caret.Column - 1;
            var caretOffset = editor.CaretOffset;
            var ForwardShowAC = false;
            var ForwardShowIS = false;
            var ISFuncNameStr = string.Empty;
            var ISFuncDescriptionStr = string.Empty;
            var ForceReSet = currentLineIndex != LastShowedLine;
            var ForceISKeepsClosed = ForceReSet;
            var xPos = int.MaxValue;
            LastShowedLine = currentLineIndex;
            var quotationCount = 0;
            var MethodAC = false;
            for (var i = 0; i < lineOffset; ++i)
            {
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

                if (quotationCount % 2 == 0)
                {
                    if (text[i] == '/')
                    {
                        if (i != 0)
                        {
                            if (text[i - 1] == '/')
                            {
                                HideISAC();
                                return;
                            }
                        }
                    }
                }
            }

            foreach (var c in text)
            {
                if (c == '#')
                {
                    string[] prep = { "define", "pragma", "file", "if" };
                    acEntrys = ACNode.ConvertFromStringArray(prep, false, "#").ToArray();
                    // HideISAC();
                    break;
                }

                if (!char.IsWhiteSpace(c))
                {
                    break;
                }
            }

            var mc = multilineCommentRegex.Matches(editor.Text,
                0); //it hurts me to do it here..but i have no other choice...
            var mlcCount = mc.Count;
            for (var i = 0; i < mlcCount; ++i)
            {
                if (caretOffset >= mc[i].Index)
                {
                    if (caretOffset <= mc[i].Index + mc[i].Length)
                    {
                        HideISAC();
                        return;
                    }
                }
                else
                {
                    break;
                }
            }

            if (lineOffset > 0)
            {
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

                        var FoundMatch = false;
                        var searchIndex = i;
                        for (var j = 0; j < ISMatches.Count; ++j)
                        {
                            if (searchIndex >= ISMatches[j].Index &&
                                searchIndex <= ISMatches[j].Index + ISMatches[j].Length)
                            {
                                FoundMatch = true;
                                var testString = ISMatches[j].Groups["name"].Value;
                                var classString = ISMatches[j].Groups["class"].Value;
                                if (classString.Length > 0)
                                {
                                    var methodString = ISMatches[j].Groups["method"].Value;
                                    var found = false;

                                    // Match for static methods.
                                    var staticMethodMap = methodMaps.FirstOrDefault(e => e.Name == classString);
                                    var staticMethod =
                                        staticMethodMap?.Methods.FirstOrDefault(e => e.Name == methodString);
                                    if (staticMethod != null)
                                    {
                                        xPos = ISMatches[j].Groups["method"].Index +
                                               ISMatches[j].Groups["method"].Length;
                                        ForwardShowIS = true;
                                        ISFuncNameStr = staticMethod.FullName;
                                        ISFuncDescriptionStr = staticMethod.CommentString;
                                        ForceReSet = true;
                                        found = true;
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
                                                ForwardShowIS = true;
                                                ISFuncNameStr = method.FullName;
                                                ISFuncDescriptionStr = method.CommentString;
                                                ForceReSet = true;
                                                found = true;
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
                                            ForwardShowIS = true;
                                            ISFuncNameStr = method.FullName;
                                            ISFuncDescriptionStr = method.CommentString;
                                            ForceReSet = true;
                                        }
                                    }
                                }
                                else
                                {
                                    var func = funcs.FirstOrDefault(e => e.Name == testString);
                                    if (func != null)
                                    {
                                        xPos = ISMatches[j].Groups["name"].Index +
                                               ISMatches[j].Groups["name"].Length;
                                        ForwardShowIS = true;
                                        ISFuncNameStr = func.FullName;
                                        ISFuncDescriptionStr = func.CommentString;
                                        ForceReSet = true;
                                    }
                                }

                                break;
                            }
                        }

                        if (FoundMatch)
                        {
                            // ReSharper disable once RedundantAssignment
                            scopeLevel--; //i have no idea why this works...
                            break;
                        }
                    }
                }

                #endregion

                #region AC

                if (IsValidFunctionChar(text[lineOffset - 1]) && quotationCount % 2 == 0)
                {
                    var IsNextCharValid = true;
                    if (text.Length > lineOffset)
                    {
                        if (IsValidFunctionChar(text[lineOffset]) || text[lineOffset] == '(')
                        {
                            IsNextCharValid = false;
                        }
                    }

                    if (IsNextCharValid)
                    {
                        var endOffset = lineOffset - 1;
                        for (var i = endOffset; i >= 0; --i)
                        {
                            if (!IsValidFunctionChar(text[i]))
                            {
                                if (text[i] == '.')
                                {
                                    MethodAC = true;
                                }

                                break;
                            }

                            endOffset = i;
                        }

                        var testString = text.Substring(endOffset, lineOffset - 1 - endOffset + 1);
                        if (testString.Length > 0)
                        {
                            if (MethodAC)
                            {
                                for (var i = 0; i < isEntrys.Length; ++i)
                                {
                                    if (isEntrys[i].EntryName.StartsWith(testString,
                                        StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        if (testString != isEntrys[i].EntryName)
                                        {
                                            ForwardShowAC = true;
                                            MethodAutoCompleteBox.SelectedIndex = i;
                                            MethodAutoCompleteBox.ScrollIntoView(MethodAutoCompleteBox.SelectedItem);
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                for (var i = 0; i < acEntrys.Length; ++i)
                                {
                                    if (acEntrys[i].EntryName.StartsWith(testString,
                                        StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        if (testString != acEntrys[i].EntryName)
                                        {
                                            ForwardShowAC = true;
                                            AutoCompleteBox.SelectedIndex = i;
                                            AutoCompleteBox.ScrollIntoView(AutoCompleteBox.SelectedItem);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                #endregion
            }

            if (!ForwardShowAC)
            {
                if (ForceISKeepsClosed)
                {
                    ForwardShowIS = false;
                }
            }

            if (ForwardShowAC | ForwardShowIS)
            {
                if (ForwardShowAC)
                {
                    ShowAC(!MethodAC);
                }
                else
                {
                    HideAC();
                }

                if (ForwardShowIS)
                {
                    ShowIS(ISFuncNameStr, ISFuncDescriptionStr);
                }
                else
                {
                    HideIS();
                }

                if (ForceReSet && ISAC_Open)
                {
                    SetISACPosition(xPos);
                }

                ShowISAC(xPos);
            }
            else
            {
                HideISAC();
            }
        }

        private bool ISAC_EvaluateKeyDownEvent(Key k)
        {
            if (ISAC_Open && AC_Open)
            {
                switch (k)
                {
                    case Key.Enter:
                        {
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
                            if (AC_IsFuncC)
                            {
                                replaceString = ((ACNode)AutoCompleteBox.SelectedItem).EntryName;
                                if (acEntrys[AutoCompleteBox.SelectedIndex].IsExecuteable)
                                {
                                    replaceString += "(" + (Program.OptionsObject.Editor_AutoCloseBrackets ? ")" : "");
                                    setCaret = true;
                                }
                            }
                            else
                            {
                                replaceString = ((ISNode)MethodAutoCompleteBox.SelectedItem).EntryName;
                                if (isEntrys[MethodAutoCompleteBox.SelectedIndex].IsExecuteable)
                                {
                                    replaceString += "(" + (Program.OptionsObject.Editor_AutoCloseBrackets ? ")" : "");
                                    setCaret = true;
                                }
                            }

                            editor.Document.Replace(endOffset, length + 1, replaceString);
                            if (setCaret)
                            {
                                editor.CaretOffset -= 1;
                            }

                            return true;
                        }
                    case Key.Up:
                        {
                            if (AC_IsFuncC)
                            {
                                AutoCompleteBox.SelectedIndex = Math.Max(0, AutoCompleteBox.SelectedIndex - 1);
                                AutoCompleteBox.ScrollIntoView(AutoCompleteBox.SelectedItem);
                            }
                            else
                            {
                                MethodAutoCompleteBox.SelectedIndex = Math.Max(0, MethodAutoCompleteBox.SelectedIndex - 1);
                                MethodAutoCompleteBox.ScrollIntoView(MethodAutoCompleteBox.SelectedItem);
                            }

                            return true;
                        }
                    case Key.Down:
                        {
                            if (AC_IsFuncC)
                            {
                                AutoCompleteBox.SelectedIndex = Math.Min(AutoCompleteBox.Items.Count - 1,
                                    AutoCompleteBox.SelectedIndex + 1);
                                AutoCompleteBox.ScrollIntoView(AutoCompleteBox.SelectedItem);
                            }
                            else
                            {
                                MethodAutoCompleteBox.SelectedIndex = Math.Min(MethodAutoCompleteBox.Items.Count - 1,
                                    MethodAutoCompleteBox.SelectedIndex + 1);
                                MethodAutoCompleteBox.ScrollIntoView(MethodAutoCompleteBox.SelectedItem);
                            }

                            return true;
                        }
                    case Key.Escape:
                        {
                            HideISAC();
                            return true;
                        }
                }
            }

            return false;
        }

        private void ShowISAC(int forcedXPos = int.MaxValue)
        {
            if (!ISAC_Open)
            {
                ISAC_Open = true;
                ISAC_Grid.Visibility = Visibility.Visible;
                SetISACPosition(forcedXPos);
                if (Program.OptionsObject.UI_Animations)
                {
                    FadeISACIn.Begin();
                }
                else
                {
                    ISAC_Grid.Opacity = 1.0;
                }
            }
        }

        private void HideISAC()
        {
            if (ISAC_Open)
            {
                ISAC_Open = false;
                if (Program.OptionsObject.UI_Animations)
                {
                    FadeISACOut.Begin();
                }
                else
                {
                    ISAC_Grid.Opacity = 0.0;
                    ISAC_Grid.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void ShowAC(bool IsFunc)
        {
            if (!AC_Open)
            {
                AC_Open = true;
                ACBorder.Height = 175.0;
                if (Program.OptionsObject.UI_Animations)
                {
                    FadeACIn.Begin();
                }
                else
                {
                    AutoCompleteBox.Width = 260.0;
                    MethodAutoCompleteBox.Width = 260.0;
                }
            }

            if (!(IsFunc && AC_IsFuncC) && AC_Open)
            {
                if (IsFunc)
                {
                    if (!AC_IsFuncC)
                    {
                        AC_IsFuncC = true;
                        if (Program.OptionsObject.UI_Animations)
                        {
                            FadeAC_FuncC_In.Begin();
                        }
                        else
                        {
                            AutoCompleteBox.Opacity = 1.0;
                            MethodAutoCompleteBox.Opacity = 0.0;
                        }
                    }
                }
                else
                {
                    if (AC_IsFuncC)
                    {
                        AC_IsFuncC = false;
                        if (Program.OptionsObject.UI_Animations)
                        {
                            FadeAC_MethodC_In.Begin();
                        }
                        else
                        {
                            AutoCompleteBox.Opacity = 0.0;
                            MethodAutoCompleteBox.Opacity = 1.0;
                        }
                    }
                }
            }
        }

        private void HideAC()
        {
            if (AC_Open)
            {
                AC_Open = false;
                if (Program.OptionsObject.UI_Animations)
                {
                    FadeACOut.Begin();
                }
                else
                {
                    AutoCompleteBox.Width = 0.0;
                    MethodAutoCompleteBox.Width = 0.0;
                    ACBorder.Height = 0.0;
                }
            }
        }

        private void ShowIS(string FuncName, string FuncDescription)
        {
            if (!IS_Open)
            {
                IS_Open = true;
                ISenseColumn.Width = new GridLength(0.0, GridUnitType.Auto);
            }

            IS_FuncName.Text = FuncName;
            IS_FuncDescription.Text = FuncDescription;
        }

        private void HideIS()
        {
            if (IS_Open)
            {
                IS_Open = false;
                ISenseColumn.Width = new GridLength(0.0);
            }

            IS_FuncDescription.Text = string.Empty;
            IS_FuncName.Text = string.Empty;
        }

        private void SetISACPosition(int forcedXPos = int.MaxValue)
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

            IS_FuncDescription.Measure(new Size(double.MaxValue, double.MaxValue));
            var y = p.Y;
            var ISACHeight = 0.0;
            if (AC_Open && IS_Open)
            {
                var ISHeight = IS_FuncDescription.DesiredSize.Height;
                ISACHeight = Math.Max(175.0, ISHeight);
            }
            else if (AC_Open)
            {
                ISACHeight = 175.0;
            }
            else if (IS_Open)
            {
                ISACHeight = IS_FuncDescription.DesiredSize.Height;
            }

            if (y + ISACHeight > editor.ActualHeight)
            {
                y = (editor.TextArea.TextView.GetVisualPosition(editor.TextArea.Caret.Position,
                    VisualYPosition.LineTop) - editor.TextArea.TextView.ScrollOffset).Y;
                y -= ISACHeight;
            }

            ISAC_Grid.Margin =
                new Thickness(p.X + ((LineNumberMargin)editor.TextArea.LeftMargins[0]).ActualWidth + 20.0, y, 0.0,
                    0.0);
        }

        private void FadeISACOut_Completed(object sender, EventArgs e)
        {
            ISAC_Grid.Visibility = Visibility.Collapsed;
        }

        private void FadeACOut_Completed(object sender, EventArgs e)
        {
            if (FadeACIn.GetCurrentState() != ClockState.Active)
            {
                ACBorder.Height = 0.0;
            }
        }

        private bool IsValidFunctionChar(char c)
        {
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_')
            {
                return true;
            }

            return false;
        }
    }
}