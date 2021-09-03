using System;
using System.Windows.Controls;

namespace SPCode.Interop
{
    public static class LoggingControl
    {
        public static TextBox LogBox;
        public static void LogAction(string message)
        {
            LogBox.Text += $"[{DateTime.Now:HH:mm:ss}] {message} \r\n";
        }
    }
}
