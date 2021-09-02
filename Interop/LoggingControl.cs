using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
