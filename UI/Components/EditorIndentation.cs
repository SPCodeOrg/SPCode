using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Indentation;

namespace SPCode.UI.Components
{
    public class EditorIndentationStrategy : IIndentationStrategy
    {
        public void IndentLine(TextDocument document, DocumentLine line)
        {
            if (document == null || line == null)
            {
                return;
            }
            DocumentLine previousLine = line.PreviousLine;
            if (previousLine != null)
            {
                ISegment indentationSegment = TextUtilities.GetWhitespaceAfter(document, previousLine.Offset);
                string indentation = document.GetText(indentationSegment);
                if (Program.OptionsObject.Editor_AgressiveIndentation)
                {
                    string currentLineTextTrimmed = (document.GetText(line)).Trim();
                    string lastLineTextTrimmed = (document.GetText(previousLine)).Trim();
                    char currentLineFirstNonWhitespaceChar = ' ';
                    if (currentLineTextTrimmed.Length > 0)
                    {
                        currentLineFirstNonWhitespaceChar = currentLineTextTrimmed[0];
                    }
                    char lastLineLastNonWhitespaceChar = ' ';
                    if (lastLineTextTrimmed.Length > 0)
                    {
                        lastLineLastNonWhitespaceChar = lastLineTextTrimmed[lastLineTextTrimmed.Length - 1];
                    }
                    if (lastLineLastNonWhitespaceChar == '{' && currentLineFirstNonWhitespaceChar != '}')
                    {
                        indentation += Program.Indentation;
                    }
                    else if (currentLineFirstNonWhitespaceChar == '}')
                    {
                        if (indentation.Length > 0)
                        {
                            indentation = indentation.Substring(0, indentation.Length) + Program.Indentation + "\n" + indentation.Substring(0, indentation.Length);
                        }
                        else
                        {
                            indentation = Program.Indentation + "\n";
                        }
                    }
                    /*if (lastLineTextTrimmed == "{" && currentLineTextTrimmed != "}")
                    {
                        indentation += "\t";
                    }
                    else if (currentLineTextTrimmed == "}")
                    {
                        if (indentation.Length > 0)
                        {
                            indentation = indentation.Substring(0, indentation.Length - 1);
                        }
                        else
                        {
                            indentation = string.Empty;
                        }
                    }*/
                }
                indentationSegment = TextUtilities.GetWhitespaceAfter(document, line.Offset);
                document.Replace(indentationSegment, indentation);
            }
        }


        public void IndentLines(TextDocument document, int beginLine, int endLine)
        { }
    }
}
