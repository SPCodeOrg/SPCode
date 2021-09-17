using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using SPCode.Utils;

namespace SPCode.UI.Components
{
    public partial class EditorElement
    {

        public void Lint(object sender, EventArgs e)
        {
            try
            {
                LinterTimer.Stop();
                Save();
                FileInfo spCompInfo = null;
                var file = FullFilePath;
                var fileInfo = new FileInfo(file);
                var currentConfig = Program.Configs[Program.SelectedConfig];
                var errorFilterRegex = new Regex(Constants.ErrorFilterRegex,
                  RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline);

                foreach (var dir in currentConfig.SMDirectories)
                {
                    spCompInfo = new FileInfo(Path.Combine(dir, "spcomp.exe"));
                    if (spCompInfo.Exists)
                    {
                        break;
                    }
                }

                if (fileInfo.Exists)
                {
                    var process = new Process();
                    process.StartInfo.WorkingDirectory = fileInfo.DirectoryName ?? throw new NullReferenceException();
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.FileName = spCompInfo.FullName;

                    var outFile = Path.Combine(fileInfo.DirectoryName, fileInfo.Name + ".smx");

                    if (File.Exists(outFile))
                    {
                        File.Delete(outFile);
                    }

                    var errorFile = $@"{fileInfo.DirectoryName}\error_{Environment.TickCount}_{file.GetHashCode():X}.txt";

                    if (File.Exists(errorFile))
                    {
                        File.Delete(errorFile);
                    }

                    var includeDirectories = new StringBuilder();

                    foreach (var dir in currentConfig.SMDirectories)
                    {
                        includeDirectories.Append(" -i=\"" + dir + "\"");
                    }

                    var includeStr = includeDirectories.ToString();

                    process.StartInfo.Arguments =
                        "\"" + fileInfo.FullName + "\" -o=\"" + outFile + "\" -e=\"" + errorFile + "\"" +
                        includeStr + " -O=" + currentConfig.OptimizeLevel + " -v=" + currentConfig.VerboseLevel;

                    try
                    {
                        process.Start();
                        process.WaitForExit();
                    }
                    catch (Exception)
                    {

                    }

                    if (File.Exists(errorFile))
                    {
                        var errorStr = File.ReadAllText(errorFile);
                        var mc = errorFilterRegex.Matches(errorStr);
                        for (var j = 0; j < mc.Count; ++j)
                        {
                            var errorLine = editor.Document.GetLineByNumber(int.Parse(mc[j].Groups["Line"].Value));
                            AddMarkerFromSelectionClick(errorLine.Offset, errorLine.Length);
                        }
                        File.Delete(errorFile);
                    }
                }
            }
            catch (Exception)
            {

            }
            
        }

        public void AddMarkerFromSelectionClick(int startOffset, int length)
        {
            ITextMarker marker = textMarkerService.Create(startOffset, length);
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
