using System;
using System.Collections.Generic;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace SPCode.UI.Components
{
    public class BracketHighlightRenderer : IBackgroundRenderer
    {
        private BracketSearchResult result;
        private readonly Brush backgroundBrush = new SolidColorBrush(Color.FromArgb(0x40, 0x88, 0x88, 0x88));
        private readonly TextView textView;

        public void SetHighlight(BracketSearchResult result)
        {
            if (this.result != result)
            {
                this.result = result;
                textView.InvalidateLayer(Layer);
            }
        }

        public BracketHighlightRenderer(TextView textView)
        {
            this.textView = textView ?? throw new ArgumentNullException("textView");

            this.textView.BackgroundRenderers.Add(this);
        }

        public KnownLayer Layer => KnownLayer.Selection;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (result == null)
            {
                return;
            }

            var builder = new BackgroundGeometryBuilder
            {
                CornerRadius = 1,
                AlignToWholePixels = true,
                BorderThickness = 0.0
            };

            builder.AddSegment(textView, new TextSegment() { StartOffset = result.OpeningBracketOffset, Length = result.OpeningBracketLength });
            builder.CloseFigure();
            builder.AddSegment(textView, new TextSegment() { StartOffset = result.ClosingBracketOffset, Length = result.ClosingBracketLength });

            var geometry = builder.CreateGeometry();
            if (geometry != null)
            {
                drawingContext.DrawGeometry(backgroundBrush, null, geometry);
            }
        }
    }

    public interface IBracketSearcher
    {
        BracketSearchResult SearchBracket(IDocument document, int offset);
    }

    public class BracketSearchResult
    {
        public int OpeningBracketOffset { get; private set; }

        public int OpeningBracketLength { get; private set; }

        public int ClosingBracketOffset { get; private set; }

        public int ClosingBracketLength { get; private set; }

        public int DefinitionHeaderOffset { get; set; }

        public int DefinitionHeaderLength { get; set; }

        public BracketSearchResult(int openingBracketOffset, int openingBracketLength,
                                   int closingBracketOffset, int closingBracketLength)
        {
            OpeningBracketOffset = openingBracketOffset;
            OpeningBracketLength = openingBracketLength;
            ClosingBracketOffset = closingBracketOffset;
            ClosingBracketLength = closingBracketLength;
        }
    }

    public class SPBracketSearcher : IBracketSearcher
    {
        #region Variables
        private readonly string openingBrackets = "([{";
        private readonly string closingBrackets = ")]}";
        private bool isCommentBlockForward;
        private bool isCommentBlockBackward;
        private bool isCommentLine;
        private bool isString;
        private bool isChar;
        #endregion

        public BracketSearchResult SearchBracket(IDocument document, int offset)
        {
            if (offset > 0)
            {
                var c = document.GetCharAt(offset - 1);
                var index = openingBrackets.IndexOf(c);
                var otherOffset = -1;
                if (index > -1)
                {
                    otherOffset = SearchBracketForward(document, offset, openingBrackets[index], closingBrackets[index]);
                }

                index = closingBrackets.IndexOf(c);
                if (index > -1)
                {
                    otherOffset = SearchBracketBackward(document, offset - 2, openingBrackets[index], closingBrackets[index]);
                }

                if (otherOffset > -1)
                {
                    var result = new BracketSearchResult(Math.Min(offset - 1, otherOffset), 1,
                                                         Math.Max(offset - 1, otherOffset), 1);
                    SearchDefinition(document, result);
                    return result;
                }
            }

            return null;
        }

        private void SearchDefinition(IDocument document, BracketSearchResult result)
        {
            if (document.GetCharAt(result.OpeningBracketOffset) != '{')
            {
                return;
            }

            var documentLine = document.GetLineByOffset(result.OpeningBracketOffset);
            while (documentLine != null && IsBracketOnly(document, documentLine))
            {
                documentLine = documentLine.PreviousLine;
            }

            if (documentLine != null)
            {
                result.DefinitionHeaderOffset = documentLine.Offset;
                result.DefinitionHeaderLength = documentLine.Length;
            }
        }

        private bool IsBracketOnly(IDocument document, IDocumentLine documentLine)
        {
            var lineText = document.GetText(documentLine).Trim();
            return lineText == "{" || string.IsNullOrEmpty(lineText)
                || lineText.StartsWith("//", StringComparison.Ordinal)
                || lineText.StartsWith("/*", StringComparison.Ordinal)
                || lineText.StartsWith("*", StringComparison.Ordinal)
                || lineText.StartsWith("'", StringComparison.Ordinal);
        }

        #region SearchBracket helper functions
        private static int ScanLineStart(IDocument document, int offset)
        {
            for (var i = offset - 1; i > 0; --i)
            {
                if (document.GetCharAt(i) == '\n')
                {
                    return i + 1;
                }
            }
            return 0;
        }

        private static int GetStartType(IDocument document, int linestart, int offset)
        {
            var inString = false;
            var inChar = false;
            var verbatim = false;
            var result = 0;
            for (var i = linestart; i < offset; i++)
            {
                switch (document.GetCharAt(i))
                {
                    case '/':
                        if (!inString && !inChar && i + 1 < document.TextLength)
                        {
                            if (document.GetCharAt(i + 1) == '/')
                            {
                                result = 1;
                            }
                        }
                        break;
                    case '"':
                        if (!inChar)
                        {
                            if (inString && verbatim)
                            {
                                if (i + 1 < document.TextLength && document.GetCharAt(i + 1) == '"')
                                {
                                    ++i;
                                    inString = false;
                                }
                                else
                                {
                                    verbatim = false;
                                }
                            }
                            else if (!inString && i > 0 && document.GetCharAt(i - 1) == '@')
                            {
                                verbatim = true;
                            }
                            inString = !inString;
                        }
                        break;
                    case '\'':
                        if (!inString)
                        {
                            inChar = !inChar;
                        }

                        break;
                    case '\\':
                        if ((inString && !verbatim) || inChar)
                        {
                            ++i;
                        }

                        break;
                }
            }

            return (inString || inChar) ? 2 : result;
        }
        #endregion

        #region Bracket Searchers
        private int SearchBracketForward(IDocument document, int offset, char openBracket, char closingBracket)
        {
            var inString = false;
            var inChar = false;
            var verbatim = false;

            var lineComment = false;
            var blockComment = false;

            if (offset < 0)
            {
                return -1;
            }

            // After searching for quick result, we check for
            // comment blocks, comment lines, string and chars

            var quickResult = QuickSearchBracketForward(document, offset, openBracket, closingBracket);
            isCommentBlockForward = CheckForCommentBlockForward(document, offset);
            isCommentLine = CheckForCommentLine(document, offset);
            isString = CheckForString(document, offset);
            isChar = CheckForChar(document, offset);

            // If the Quick Search result is valid, only return it if we determined the bracket
            // is valid (is not inside a comment block, comment line, string or char)

            if (quickResult >= 0 && !isCommentBlockForward && !isCommentLine && !isString && !isChar)
            {
                return quickResult;
            }

            // Otherwise, scan normally (not sure how it's done, I need to review and add further comments)

            var linestart = ScanLineStart(document, offset);

            var starttype = GetStartType(document, linestart, offset);
            if (starttype != 0)
            {
                return -1;
            }

            var brackets = 1;

            while (offset < document.TextLength)
            {
                var ch = document.GetCharAt(offset);
                switch (ch)
                {
                    case '\r':
                    case '\n':
                        lineComment = false;
                        inChar = false;
                        if (!verbatim)
                        {
                            inString = false;
                        }

                        break;
                    case '/':
                        if (blockComment)
                        {
                            if (document.GetCharAt(offset - 1) == '*')
                            {
                                blockComment = false;
                            }
                        }
                        if (!inString && !inChar && offset + 1 < document.TextLength)
                        {
                            if (!blockComment && document.GetCharAt(offset + 1) == '/')
                            {
                                lineComment = true;
                            }
                            if (!lineComment && document.GetCharAt(offset + 1) == '*')
                            {
                                blockComment = true;
                            }
                        }
                        break;
                    case '"':
                        if (!(inChar || lineComment || blockComment))
                        {
                            if (inString && verbatim)
                            {
                                if (offset + 1 < document.TextLength && document.GetCharAt(offset + 1) == '"')
                                {
                                    ++offset; // skip escaped quote
                                    inString = false; // let the string go
                                }
                                else
                                {
                                    verbatim = false;
                                }
                            }
                            else if (!inString && offset > 0 && document.GetCharAt(offset - 1) == '@')
                            {
                                verbatim = true;
                            }
                            inString = !inString;
                        }
                        break;
                    case '\'':
                        if (!(inString || lineComment || blockComment))
                        {
                            inChar = !inChar;
                        }
                        break;
                    case '\\':
                        if ((inString && !verbatim) || inChar)
                        {
                            ++offset; // skip next character
                        }

                        break;
                    default:
                        if (ch == openBracket)
                        {
                            if (!(inString || inChar || lineComment || blockComment))
                            {
                                ++brackets;
                            }
                        }
                        else if (ch == closingBracket)
                        {
                            if (!(inString || inChar || lineComment || blockComment || isCommentBlockForward || isCommentLine || isString || isChar))
                            {
                                --brackets;
                                if (brackets == 0)
                                {
                                    return offset;
                                }
                            }
                        }
                        break;
                }
                ++offset;
            }
            return -1;
        }

        private int SearchBracketBackward(IDocument document, int offset, char openBracket, char closingBracket)
        {
            if (offset + 1 >= document.TextLength)
            {
                return -1;
            }

            // After searching for quick result, we check for
            // comment blocks, comment lines, string and chars

            var quickResult = QuickSearchBracketBackward(document, offset, openBracket, closingBracket);
            isCommentBlockBackward = CheckForCommentBackward(document, offset, openBracket, closingBracket);
            isCommentLine = CheckForCommentLine(document, offset);
            isString = CheckForString(document, offset);
            isChar = CheckForChar(document, offset);

            // If the Quick Search result is valid, only return it if we determined the bracket
            // is valid (is not inside a comment block, comment line, string or char)

            if (quickResult >= 0 && !isCommentBlockBackward && !isCommentLine && !isString && !isChar)
            {
                return quickResult;
            }

            // Otherwise, scan normally (not sure how it's done, I need to review and add further comments)

            var linestart = ScanLineStart(document, offset + 1);

            var starttype = GetStartType(document, linestart, offset + 1);
            if (starttype == 1)
            {
                return -1;
            }
            var bracketStack = new Stack<int>();
            var blockComment = false;
            var lineComment = false;
            var inChar = false;
            var inString = false;
            var verbatim = false;

            for (var i = 0; i <= offset; ++i)
            {
                var ch = document.GetCharAt(i);
                switch (ch)
                {
                    case '\r':
                    case '\n':
                        lineComment = false;
                        inChar = false;
                        if (!verbatim)
                        {
                            inString = false;
                        }

                        break;
                    case '/':
                        if (blockComment)
                        {
                            if (document.GetCharAt(i - 1) == '*')
                            {
                                blockComment = false;
                            }
                        }
                        if (!inString && !inChar && i + 1 < document.TextLength)
                        {
                            if (!blockComment && document.GetCharAt(i + 1) == '/')
                            {
                                lineComment = true;
                            }
                            if (!lineComment && document.GetCharAt(i + 1) == '*')
                            {
                                blockComment = true;
                            }
                        }
                        break;
                    case '"':
                        if (!(inChar || lineComment || blockComment))
                        {
                            if (inString && verbatim)
                            {
                                if (i + 1 < document.TextLength && document.GetCharAt(i + 1) == '"')
                                {
                                    ++i; // skip escaped quote
                                    inString = false; // let the string go
                                }
                                else
                                {
                                    verbatim = false;
                                }
                            }
                            else if (i > 0) //FIX CRASH ON SELECTING
                            {
                                if (!inString && offset > 0 && document.GetCharAt(i - 1) == '@')
                                {
                                    verbatim = true;
                                }
                            }
                            inString = !inString;
                        }
                        break;
                    case '\'':
                        if (!(inString || lineComment || blockComment))
                        {
                            inChar = !inChar;
                        }
                        break;
                    case '\\':
                        if ((inString && !verbatim) || inChar)
                        {
                            ++i;
                        }

                        break;
                    default:
                        if (ch == openBracket)
                        {
                            if (!(inString || inChar || lineComment || blockComment || isCommentBlockBackward || isCommentLine || isString || isChar))
                            {
                                bracketStack.Push(i);
                            }
                        }
                        else if (ch == closingBracket)
                        {
                            if (!(inString || inChar || lineComment || blockComment))
                            {
                                if (bracketStack.Count > 0)
                                {
                                    bracketStack.Pop();
                                }
                            }
                        }
                        break;
                }
            }
            if (bracketStack.Count > 0)
            {
                return bracketStack.Pop();
            }

            return -1;
        }
        #endregion

        #region Quick Search Helpers
        private int QuickSearchBracketBackward(IDocument document, int offset, char openBracket, char closingBracket)
        {
            var brackets = -1;
            for (var i = offset; i >= 0; --i)
            {
                var ch = document.GetCharAt(i);
                if (ch == openBracket)
                {
                    ++brackets;
                    if (brackets == 0)
                    {
                        return i;
                    }
                }
                else if (ch == closingBracket)
                {
                    --brackets;
                }
                else if (ch == '"')
                {
                    break;
                }
                else if (ch == '\'')
                {
                    break;
                }
                else if (ch == '/' && i > 0)
                {
                    if (document.GetCharAt(i - 1) == '/')
                    {
                        break;
                    }

                    if (document.GetCharAt(i - 1) == '*')
                    {
                        break;
                    }
                }
            }
            return -1;
        }

        private int QuickSearchBracketForward(IDocument document, int offset, char openBracket, char closingBracket)
        {
            var brackets = 1;
            // try "quick find" - find the matching bracket if there is no string/comment in the way
            for (var i = offset; i < document.TextLength; ++i)
            {
                var ch = document.GetCharAt(i);
                if (ch == openBracket)
                {
                    ++brackets;
                }
                else if (ch == closingBracket)
                {
                    --brackets;
                    if (brackets == 0)
                    {
                        return i;
                    }
                }
                else if (ch == '"')
                {
                    break;
                }
                else if (ch == '\'')
                {
                    break;
                }
                else if (ch == '/' && i > 0)
                {
                    if (document.GetCharAt(i - 1) == '/')
                    {
                        break;
                    }
                }
                else if (ch == '*' && i > 0)
                {
                    if (document.GetCharAt(i - 1) == '/')
                    {
                        break;
                    }
                }
            }
            return -1;
        }

        private bool CheckForCommentBlockForward(IDocument document, int offset)
        {
            for (var i = offset; i < document.TextLength; ++i)
            {
                var ch = document.GetCharAt(i);

                // If we find the characters ' */ ' together scanning forward, 
                // it means a comment block is finishing
                // therefore, we've been inside a code block this whole time:
                // this bracket should be ignored by the highlighter

                if (ch == '*' && document.GetCharAt(i + 1) == '/')
                {
                    return true;
                }

                // If we find, however, ' /* ', a code block is starting:
                // not possible that we've been in a comment block
                // that's a nested comment compiling error

                if (ch == '/' && document.GetCharAt(i + 1) == '*')
                {
                    return false;
                }

            }
            return false;
        }

        private bool CheckForCommentBackward(IDocument document, int offset, char openBracket, char closingBracket)
        {
            for (var i = offset; i >= 0; --i)
            {
                var ch = document.GetCharAt(i);

                // If we find the characters ' /* ' together while scanning backwards, 
                // it means a comment block is starting
                // therefore, we've been inside a code block this whole time:
                // this bracket should be ignored by the highlighter

                if (ch == '*' && document.GetCharAt(i - 1) == '/')
                {
                    return true;
                }

                // If we find, however, ' /* ', a code block is finishing:
                // not possible that we've been in a comment block
                // that's a nested comment compiling error

                if (ch == '/' && i > 0 && document.GetCharAt(i - 1) == '*')
                {
                    return false;
                }

            }
            return false;
        }

        private bool CheckForCommentLine(IDocument document, int offset)
        {
            for (var i = offset; i >= 0; --i)
            {
                var ch = document.GetCharAt(i);

                // If we find two ' // ' together as we scan backwards
                // we find we are in a comment line, and should ignore the bracket

                if (ch == '/' && document.GetCharAt(i - 1) == '/')
                {
                    return true;
                }

                // If the next scanned character is a newline, cut the function off

                if (ch == '\n')
                {
                    break;
                }
            }
            return false;
        }

        private bool CheckForString(IDocument document, int offset)
        {
            var quoteFound = false;
            for (var i = offset; i >= 0; --i)
            {
                var ch = document.GetCharAt(i);

                // If we find a quote in the same line, set a flag.
                
                if (ch == '"')
                {
                    quoteFound = true;
                }

                // Otherwise, keep looking for a line jump and a '\'
                // to cover the case of its usage to escape the newline

                if (ch == '\n' && i > 0)
                {
                    if (document.GetCharAt(i - 1) == '\r' && document.GetCharAt(i - 2) == '\\')
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // If there was an opening quote, look forward for the closing quote
            // skipping the combination of a backslash and a newline

            if (quoteFound)
            {
                for (var i = offset; i < document.TextLength; ++i)
                {
                    var ch = document.GetCharAt(i);
                    if (ch == '"')
                    {
                        return true;
                    }

                    if (ch == '\\' && document.GetCharAt(i + 1) == '\r')
                    {
                        continue;
                    }
                }
            }
            return false;
        }

        private bool CheckForChar(IDocument document, int offset)
        {
            var apFound = false;
            for (var i = offset; i >= 0; --i)
            {
                var ch = document.GetCharAt(i);

                // Scanning backwards, if we find the apostrophe, set the flag

                if (ch == '\'')
                {
                    apFound = true;
                }
            }
            if (apFound)
            {
                for (var i = offset; i < document.TextLength; ++i)
                {
                    var ch = document.GetCharAt(i);

                    // If the flag is true, scan for the other one

                    if (ch == '\'')
                    {
                        return true;
                    }
                    if (ch == '\n')
                    {
                        break;
                    }
                }
            }
            return false;
        }
        #endregion
    }
}
