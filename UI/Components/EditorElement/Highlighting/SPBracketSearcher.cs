using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;

namespace SPCode.Utils;

public class SPBracketSearcher
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

        if (offset < 0 || offset + 1 > document.TextLength)
        {
            return -1;
        }

        // After searching for quick result, we check for
        // comment blocks, comment lines, string and chars

        var quickResult = BracketHelpers.QuickSearchBracketForward(document, offset, openBracket, closingBracket);
        isCommentBlockForward = BracketHelpers.CheckForCommentBlockForward(document, offset);
        isCommentLine = BracketHelpers.CheckForCommentLine(document, offset);
        isString = BracketHelpers.CheckForString(document, offset);
        isChar = BracketHelpers.CheckForChar(document, offset);

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

        var quickResult = BracketHelpers.QuickSearchBracketBackward(document, offset, openBracket, closingBracket);
        isCommentBlockBackward = BracketHelpers.CheckForCommentBlockBackward(document, offset);
        isCommentLine = BracketHelpers.CheckForCommentLine(document, offset);
        isString = BracketHelpers.CheckForString(document, offset);
        isChar = BracketHelpers.CheckForChar(document, offset);

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


}