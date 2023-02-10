using System;
using System.Windows.Controls;

namespace SPCode.Interop;

public static class LoggingControl
{
    public static TextBox LogBox;
    public static void LogAction(string message, int newLines = 1)
    {
        try
        {
            LogBox.Text += $"[{DateTime.Now:HH:mm:ss}] {message} {new string('\n', newLines)}";
            LogBox.CaretIndex = LogBox.Text.Length;
            LogBox.ScrollToEnd();
        }
        catch (Exception)
        {

        }
    }
}