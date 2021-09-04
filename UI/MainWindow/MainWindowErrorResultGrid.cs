using System.Windows;
using System.Windows.Controls;
using SPCode.Utils;

namespace SPCode.UI
{
    public partial class MainWindow
    {
        private void ErrorResultGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var row = (ErrorDataGridRow)ErrorResultGrid.SelectedItem;
            if (row == null)
            {
                return;
            }

            var fileName = row.File;
            var editors = GetAllEditorElements();
            if (editors == null)
            {
                return;
            }

            foreach (var editor in editors)
            {
                if (editor.FullFilePath == fileName)
                {
                    editor.Parent.IsSelected = true;
                    var line = GetLineInteger(row.Line);
                    if (line > 0 && line <= editor.editor.LineCount)
                    {
                        var lineObj = editor.editor.Document.Lines[line - 1];
                        editor.editor.ScrollToLine(line - 1);
                        editor.editor.Select(lineObj.Offset, lineObj.Length);
                    }
                }
            }
        }

        private void CloseErrorResultGrid(object sender, RoutedEventArgs e)
        {
            CompileOutputRow.Height = new GridLength(8.0);
        }

        private int GetLineInteger(string lineStr)
        {
            var end = 0;
            for (var i = 0; i < lineStr.Length; ++i)
            {
                if (lineStr[i] >= '0' && lineStr[i] <= '9')
                {
                    end = i;
                }
                else
                {
                    break;
                }
            }

            if (int.TryParse(lineStr.Substring(0, end + 1), out var line))
            {
                return line;
            }

            return -1;
        }
    }
}
