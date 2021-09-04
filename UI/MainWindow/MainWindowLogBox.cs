using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SPCode.UI
{
    public partial class MainWindow
    {
        private void LogTextbox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            LogTextbox.ContextMenu = LogTextbox.Resources["LogContextMenu"] as ContextMenu;
        }
        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            LogTextbox.Clear();
        }
    }
}
