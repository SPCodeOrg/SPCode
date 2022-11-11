using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using SPCode.Interop;
using SPCode.Utils;

namespace SPCode.UI;

public partial class MainWindow
{
    public bool HideErrors = false;
    public bool HideWarnings = false;

    private void Status_ErrorButton_Clicked(object sender, RoutedEventArgs e)
    {
        var isChecked = (sender as ToggleButton).IsChecked.Value;
        HideErrors = !isChecked;
        if (CurrentErrors.Count == 0 && CurrentWarnings.Count == 0)
        {
            return;
        }

        UpdateErrorGrid();
    }

    private void Status_WarningButton_Clicked(object sender, RoutedEventArgs e)
    {
        var isChecked = (sender as ToggleButton).IsChecked.Value;
        HideWarnings = !isChecked;

        if (CurrentErrors.Count == 0 && CurrentWarnings.Count == 0)
        {
            return;
        }

        UpdateErrorGrid();
    }

    private void Status_CopyErrorsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(CurrentErrorString))
        {
            Clipboard.SetText(CurrentErrorString);
        }
    }

    private void UpdateErrorGrid()
    {
        ErrorResultGrid.Items.Clear();
        var listBuffer = new List<ErrorDataGridRow>();

        if (!HideWarnings)
        {
            listBuffer.AddRange(CurrentWarnings);
        }

        if (!HideErrors)
        {
            listBuffer.AddRange(CurrentErrors);
        }

        listBuffer.OrderBy(x => int.Parse(x.Line)).ToList().ForEach(y => ErrorResultGrid.Items.Add(y));
    }
}
