using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using MahApps.Metro;

namespace SPCode.UI.Windows
{
    public partial class GoToLineWindow
    {
        #region Variables
        private readonly TextEditor _editor;
        private readonly int _lineNumber;
        private readonly double _offsetNumber;
        #endregion

        #region Constructor
        public GoToLineWindow()
        {
            InitializeComponent();
            if (Program.OptionsObject.Program_AccentColor != "Red" || Program.OptionsObject.Program_Theme != "BaseDark")
            {
                ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor),
                    ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme));
            }
            _editor = Program.MainWindow.GetCurrentEditorElement().editor;
            _lineNumber = _editor.LineCount;
            _offsetNumber = _editor.Document.TextLength;

            Language_Translate();

            rbLineJump.Content += $" (1-{_lineNumber})";
            rbOffsetJump.Content += $" (0-{_offsetNumber})";

            JumpNumber.Focus();
            JumpNumber.SelectAll();
        }
        #endregion

        #region Events
        private void JumpNumberKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CheckInput(out var validInput);
                if (!validInput)
                {
                    return;
                }
                JumpToNumber(null, null);
                e.Handled = true;
                Close();
            }
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void JumpNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckInput(out _);
        }

        private void MetroWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void RbLineJump_Checked(object sender, RoutedEventArgs e)
        {
            CheckInput(out _);
        }

        private void RbOffsetJump_Checked(object sender, RoutedEventArgs e)
        {
            CheckInput(out _);
        }
        #endregion

        #region Methods
        private void JumpToNumber(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(JumpNumber.Text, out var num))
            {
                if (rbLineJump.IsChecked != null && rbLineJump.IsChecked.Value)
                {
                    num = Math.Max(1, Math.Min(num, _editor.LineCount));
                    var line = _editor.Document.GetLineByNumber(num);
                    if (line != null)
                    {
                        _editor.ScrollToLine(num);
                        _editor.Select(line.Offset, line.Length);
                        _editor.CaretOffset = line.Offset;
                    }
                }
                else
                {
                    num = Math.Max(0, Math.Min(num, _editor.Text.Length));
                    var line = _editor.Document.GetLineByOffset(num);
                    if (line != null)
                    {
                        _editor.ScrollTo(line.LineNumber, 0);
                        _editor.CaretOffset = num;
                    }
                }
            }

            _editor.Focus();
            Close();
        }

        private void CheckInput(out bool valid)
        {
            if (rbLineJump == null || rbOffsetJump == null)
            {
                valid = false;
                return;
            }

            var textStr = JumpNumber.Text;

            if (!int.TryParse(textStr, out var text) || string.IsNullOrEmpty(textStr))
            {
                btJump.IsEnabled = false;
                lblError.Content = "Invalid input!";
                valid = false;
                return;
            }
            else if (((bool)rbLineJump.IsChecked && text > _lineNumber) ||
                ((bool)rbOffsetJump.IsChecked && text > _offsetNumber))
            {
                btJump.IsEnabled = false;
                valid = false;
                lblError.Content = "Out of bounds!";
                return;
            }
            else
            {
                valid = true;
                btJump.IsEnabled = true;
                lblError.Content = string.Empty;
            }
        }

        public void Language_Translate()
        {
            rbLineJump.Content = Program.Translations.GetLanguage("GoToLine");
            rbOffsetJump.Content = Program.Translations.GetLanguage("GoToOffset");
            btJump.Content = Program.Translations.GetLanguage("Go");
        }
        #endregion
    }
}
