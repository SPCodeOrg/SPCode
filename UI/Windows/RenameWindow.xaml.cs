using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro;
using static SPCode.Interop.TranslationProvider;

namespace SPCode.UI.Windows
{
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
                EvaluateRTL();

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
                lblError.Content = Translate("EmptyName");
                return;
            }
            if (IsNameTaken(inputText))
            {
                lblError.Content = Translate("NameAlreadyExists");
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
                lblError.Content = Translate("FileNotSupported");
                lblError.ToolTip = Translate("FileWillBeExcluded");
            }
            else
            {
                lblError.Content = "";
                lblError.ToolTip = "";
            }
        }

        public void Language_Translate()
        {
            BtAccept.Content = Translate("Accept");
            BtCancel.Content = Translate("Cancel");
        }

        private void EvaluateRTL()
        {
            RenameWindowMainGrid.FlowDirection = Program.IsRTL ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }
        #endregion
    }
}