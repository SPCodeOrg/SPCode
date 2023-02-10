using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace SPCode.UI.Components;

public class ColorizeSelection : DocumentColorizingTransformer
{
    public bool HighlightSelection;
    public string SelectionString = string.Empty;

    protected override void ColorizeLine(DocumentLine line)
    {
        if (HighlightSelection)
        {
            if (string.IsNullOrWhiteSpace(SelectionString))
            {
                return;
            }

            var lineStartOffset = line.Offset;
            var text = CurrentContext.Document.GetText(line);
            var start = 0;
            int index;
            while ((index = text.IndexOf(SelectionString, start, StringComparison.Ordinal)) >= 0)
            {
                ChangeLinePart(
                    lineStartOffset + index,
                    lineStartOffset + index + SelectionString.Length,
                    element =>
                    {
                        element.BackgroundBrush = new SolidColorBrush(Color.FromArgb(80, 11, 95, 188));
                        //element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(Colors.White));
                    });
                start = index + 1;
            }
        }
    }
}