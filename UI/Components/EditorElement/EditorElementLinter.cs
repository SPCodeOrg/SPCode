using System.Windows.Media;
using SPCode.Utils;

namespace SPCode.UI.Components
{
    public partial class EditorElement
    {
        public void AddMarkerFromSelectionClick()
        {
            ITextMarker marker = textMarkerService.Create(editor.SelectionStart, editor.SelectionLength);
            marker.MarkerTypes = TextMarkerTypes.SquigglyUnderline;
            marker.MarkerColor = Colors.Red;
        }

        public void RemoveMarker()
        {
            textMarkerService.RemoveAll(IsSelected);
        }

        bool IsSelected(ITextMarker marker)
        {
            int selectionEndOffset = editor.SelectionStart + editor.SelectionLength;
            if (marker.StartOffset >= editor.SelectionStart && marker.StartOffset <= selectionEndOffset)
                return true;
            if (marker.EndOffset >= editor.SelectionStart && marker.EndOffset <= selectionEndOffset)
                return true;
            return false;
        }
    }
}
