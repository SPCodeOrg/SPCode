using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using SPCode.Utils;

namespace SPCode.UI.Components;

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