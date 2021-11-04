using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro;

namespace SPCode.UI.Windows;

public partial class RenameWindow
{
    #region Variables
    public string NewName = string.Empty;
    private readonly string _file;
    #endregion

    #region Constructor
    public RenameWindow(string file)
    {
        InitializeComponent();
        if (Program.OptionsObject.Program_AccentColor != "Red" || Program.OptionsObject.Program_Theme != "BaseDark")
        {
            ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor),
                ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme));
        }

        try
        {
            Language_Translate();
            _file = file;
            TxtName.Text = Path.GetFileName(_file);
            TxtName.Focus();
            TxtName.Select(0, TxtName.Text.LastIndexOf('.'));
        }
        catch (Exception)
        {
            NewName = string.Empty;
            Close();
        }
    }
    #endregion

    #region Events

    private void MetroWindow_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                NewName = string.Empty;
                Close();
                break;
            case Key.Enter:
                Submit();
                break;
        }
    }

    private void TxtName_TextChanged(object sender, TextChangedEventArgs e)
    {
        CheckExtension();
    }

    private void BtAccept_Click(object sender, RoutedEventArgs e)
    {
        Submit();
    }

    private void BtCancel_Click(object sender, RoutedEventArgs e)
    {
        NewName = string.Empty;
        Close();
    }

    #endregion

    #region Methods

    private void Submit()
    {
        var inputText = TxtName.Text;
        if (string.IsNullOrEmpty(inputText))
        {
            lblError.Content = Program.Translations.Get("EmptyName");
            return;
        }
        if (IsNameTaken(inputText))
        {
            lblError.Content = Program.Translations.Get("NameAlreadyExists");
            return;
        }
        NewName = TxtName.Text;
        Close();
    }

    private bool IsNameTaken(string inputText)
    {
        var filePath = Path.GetDirectoryName(_file);

        foreach (var file in Directory.GetFiles(filePath))
        {
            if (Path.GetFileName(file).ToLower().Equals(inputText.ToLower()))
            {
                return true;
            }
        }

        return false;
    }

    private void CheckExtension()
    {
        if (!TxtName.Text.Contains(".") || !Program.MainWindow.FileIcons.ContainsKey(TxtName.Text.Substring(TxtName.Text.LastIndexOf('.'))))
        {
            lblError.Content = Program.Translations.Get("FileNotSupported");
            lblError.ToolTip = Program.Translations.Get("FileWillBeExcluded");
        }
        else
        {
            lblError.Content = "";
            lblError.ToolTip = "";
        }
    }

    public void Language_Translate()
    {
        BtAccept.Content = Program.Translations.Get("Accept");
        BtCancel.Content = Program.Translations.Get("Cancel");
    }
    #endregion
}
