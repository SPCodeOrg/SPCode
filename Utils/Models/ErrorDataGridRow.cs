using System.Text.RegularExpressions;

namespace SPCode.Utils;

public class ErrorDataGridRow
{
    public bool IsError => Type.Contains("error");
    public bool IsWarning => Type.Contains("warning");

    public string File { get; }
    public string Line { get; }
    public string Type { get; }
    public string Details { get; }

    public ErrorDataGridRow(Match match)
    {
        File = match.Groups["File"].Value.Trim();
        Line = match.Groups["Line"].Value.Trim();
        Type = match.Groups["Type"].Value.Trim();
        Details = match.Groups["Details"].Value.Trim();
    }
}