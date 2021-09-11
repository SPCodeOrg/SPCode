using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SPCode.UI.Components;
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

            var editors = GetAllEditorElements();
            if (editors == null)
            {
                return;
            }

            // Create a file info with the supplied file 
            var fileName = row.File;
            var fInfo = new FileInfo(fileName);

            // If it doesn't exist, it's probably a local file relative to one of the scripts being compiled
            if (!fInfo.Exists)
            {
                var exists = false;

                // We're gonna search for the file containing the error
                // inside every compiled scripts location
                foreach (var script in ScriptsCompiled)
                {
                    var scriptDirInfo = Path.GetDirectoryName(script);
                    fInfo = new FileInfo(scriptDirInfo + Path.DirectorySeparatorChar + fileName);
                    if (fInfo.Exists)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    return;
                }
            }

            // Look for the file that has the error among those that are open
            foreach (var ed in editors)
            {
                if (ed.FullFilePath == fInfo.FullName)
                {
                    GoToErrorLine(ed, row);
                }
            }

            // If it's not opened, open it and go to the error line
            if (TryLoadSourceFile(fInfo.FullName, out var editor, true, false, true) && editor != null)
            {
                GoToErrorLine(editor, row);
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

        private void GoToErrorLine(EditorElement editor, ErrorDataGridRow row)
        {
            editor.Parent.IsSelected = true;
            var line = GetLineInteger(row.Line);
            if (line > 0 && line <= editor.editor.LineCount)
            {
                var lineObj = editor.editor.Document.Lines[line - 1];
                editor.editor.ScrollToLine(line - 1);
                editor.editor.Select(lineObj.Offset, lineObj.Length);
                return;
            }
        }
    }
}
