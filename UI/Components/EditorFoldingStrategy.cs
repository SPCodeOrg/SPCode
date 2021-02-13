using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace SPCode.UI.Components
{
    public class SPFoldingStrategy
    {
        public char OpeningBrace { get; set; }
        public char ClosingBrace { get; set; }

        public SPFoldingStrategy()
        {
            OpeningBrace = '{';
            ClosingBrace = '}';
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
            Stack<int> startOffsets = new Stack<int>();
            int lastNewLineOffset = 0;
            int CommentMode = 0; // 0 = None, 1 = Single, 2 = Multi, 3 = String
            for (int i = 0; i < document.TextLength; ++i)
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
                                                char oneCharAfter = document.GetCharAt(i + 1);
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
                                                int startOffset = startOffsets.Pop();
                                                if (startOffset < lastNewLineOffset)
                                                {
                                                    newFoldings.Add(new NewFolding(startOffset, i + 1));
                                                }
                                            }
                                            break;
                                        }
                                    case '\"':
                                        {
                                            CommentMode = 3;
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
                                            int startOffset = startOffsets.Pop();
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
                                if (c == '\"')
                                {
                                    CommentMode = 0;
                                }
                                break;
                            }
                    }
                }
            }

            /*Stack<int> startOffsets = new Stack<int>();
            int lastNewLineOffset = 0;
            char openingBrace = this.OpeningBrace;
            char closingBrace = this.ClosingBrace;
            for (int i = 0; i < document.TextLength; i++)
            {
                char c = document.GetCharAt(i);
                if (c == openingBrace)
                {
                    startOffsets.Push(i);
                }
                else if (c == closingBrace && startOffsets.Count > 0)
                {
                    int startOffset = startOffsets.Pop();
                    // don't fold if opening and closing brace are on the same line
                    if (startOffset < lastNewLineOffset)
                    {
                        newFoldings.Add(new NewFolding(startOffset, i + 1));
                    }
                }
                else if (c == '\n' || c == '\r')
                {
                    lastNewLineOffset = i + 1;
                }
            }*/

            newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
            return newFoldings;
        }
    }
}
