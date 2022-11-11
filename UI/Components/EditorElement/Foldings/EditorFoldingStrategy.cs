using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace SPCode.UI.Components;

public class SPFoldingStrategy
{
    public SPFoldingStrategy()
    {

    }

    public void UpdateFoldings(FoldingManager manager, TextDocument document)
    {
        var newFoldings = CreateNewFoldings(document, out var firstErrorOffset);
        manager.UpdateFoldings(newFoldings, firstErrorOffset);
    }

    public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
    {
        firstErrorOffset = -1;
        return CreateNewFoldings(document);
    }

    public IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
    {
        var newFoldings = new List<NewFolding>();
        var startOffsets = new Stack<int>();
        var lastNewLineOffset = 0;
        var CommentMode = 0; // 0 = None, 1 = Single, 2 = Multi, 3 = String, 4 = Char
        for (var i = 0; i < document.TextLength; ++i)
        {
            var c = document.GetCharAt(i);

            if (c == '\n' || c == '\r')
            {
                lastNewLineOffset = i + 1;
                if (CommentMode == 1)
                {
                    CommentMode = 0;
                }
            }
            else
            {
                switch (CommentMode)
                {
                    case 0:
                    {
                        switch (c)
                        {
                            case '/':
                            {
                                if ((i + 1) < document.TextLength)
                                {
                                    var oneCharAfter = document.GetCharAt(i + 1);
                                    if (oneCharAfter == '*')
                                    {
                                        CommentMode = 2;
                                        startOffsets.Push(i);
                                    }
                                    else if (oneCharAfter == '/')
                                    {
                                        CommentMode = 1;
                                    }
                                }
                                break;
                            }
                            case '{':
                            {
                                startOffsets.Push(i);
                                break;
                            }
                            case '}':
                            {
                                if (startOffsets.Count > 0)
                                {
                                    var startOffset = startOffsets.Pop();
                                    if (startOffset < lastNewLineOffset)
                                    {
                                        newFoldings.Add(new NewFolding(startOffset, i + 1));
                                    }
                                }
                                break;
                            }
                            case '"':
                            {
                                CommentMode = 3;
                                break;
                            }
                            case '\'':
                            {
                                CommentMode = 4;
                                break;
                            }
                        }
                        break;
                    }
                    case 2:
                    {
                        if (c == '/')
                        {
                            if (i > 0)
                            {
                                if (document.GetCharAt(i - 1) == '*')
                                {
                                    var startOffset = startOffsets.Pop();
                                    CommentMode = 0;
                                    if (startOffset < lastNewLineOffset)
                                    {
                                        newFoldings.Add(new NewFolding(startOffset, i + 1));
                                    }
                                }
                            }
                        }
                        break;
                    }
                    case 3:
                    {
                        // if quote found, search backwards for backslashes
                        if (c == '"')
                        {
                            var slashes = 0;
                            for (var j = i - 1; j >= 0; j--)
                            {
                                if (document.GetCharAt(j) == '\\')
                                {
                                    slashes++;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            // if the total amount of subsequent backslashes found is even
                            // it means the quote is not escaped
                            if (slashes % 2 == 0)
                            {
                                CommentMode = 0;
                            }
                        }
                        break;
                    }
                    case 4:
                    {
                        // if apostrophe found, search backwards for backslashes
                        if (c == '\'')
                        {
                            var slashes = 0;
                            for (var j = i - 1; j >= 0; j--)
                            {
                                if (document.GetCharAt(j) == '\\')
                                {
                                    slashes++;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            // if the total amount of subsequent backslashes found is even
                            // it means the apostrophe is not escaped
                            if (slashes % 2 == 0)
                            {
                                CommentMode = 0;
                            }
                        }
                        break;
                    }
                }
            }
        }

        newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        return newFoldings;
    }
}