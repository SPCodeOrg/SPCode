using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;

namespace SPCode.Utils;

public static class BracketHelpers
{
    private static readonly Dictionary<string, string> BracketPairs = new()
    {
        { "}", "{" },
        { "]", "[" },
        { ")", "(" }
    };

    public static int QuickSearchBracketBackward(IDocument document, int offset, char openBracket, char closingBracket)
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

    public static int QuickSearchBracketForward(IDocument document, int offset, char openBracket, char closingBracket)
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

    public static bool CheckForCommentBlockForward(IDocument document, int offset)
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

            if (ch == '/' && i + 1 < document.TextLength && document.GetCharAt(i + 1) == '*')
            {
                return false;
            }

        }
        return false;
    }

    public static bool CheckForCommentBlockBackward(IDocument document, int offset)
    {
        for (var i = offset; i >= 0; --i)
        {
            var ch = document.GetCharAt(i);

            // If we find the characters ' /* ' together while scanning backwards, 
            // it means a comment block is starting
            // therefore, we've been inside a code block this whole time:
            // this bracket should be ignored by the highlighter

            if (ch == '*' && i > 0 && document.GetCharAt(i - 1) == '/')
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

    public static bool CheckForCommentLine(IDocument document, int offset)
    {
        for (var i = offset; i >= 0; --i)
        {
            var ch = document.GetCharAt(i);

            // If we find two ' // ' together as we scan backwards
            // we find we are in a comment line, and should ignore the bracket

            if (ch == '/' && i > 0 && document.GetCharAt(i - 1) == '/')
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

    public static bool CheckForString(IDocument document, int offset)
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

            if (ch == '\n' && i > 1)
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

                if (ch == '\\' && i + 1 < document.TextLength && document.GetCharAt(i + 1) == '\r')
                {
                    continue;
                }
            }
        }
        return false;
    }

    public static bool CheckForChar(IDocument document, int offset)
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

    /// <summary>
    /// Utility to determine whether to place a closing bracket if the editor has already done that automatically.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="offset"></param>
    /// <param name="bracket"></param>
    /// <returns></returns>
    public static bool CheckForClosingBracket(IDocument document, int offset, string bracket)
    {
        return
            document.GetCharAt(offset).ToString() == bracket &&
            document.GetCharAt(offset - 1).ToString() == BracketPairs[bracket];
    }
}