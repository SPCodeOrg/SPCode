using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SPCode.Utils;

namespace SPCode.UI.Components;

public partial class HotkeyEditorControl
{
    public static readonly DependencyProperty HotkeyProperty =
        DependencyProperty.Register(nameof(Hotkey), typeof(Hotkey),
            typeof(HotkeyEditorControl),
            new FrameworkPropertyMetadata(default(Hotkey),
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    private bool InputEnabled = true;

    public Hotkey Hotkey
    {
        get => (Hotkey)GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    public HotkeyEditorControl()
    {
        InitializeComponent();
    }

    private async void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Don't let the event pass further
        // because we don't want standard textbox shortcuts working
        e.Handled = true;

        // Get modifiers and key data
        var key = e.Key;
        var modifiers = Keyboard.Modifiers;

        // When Alt is pressed, SystemKey is used instead
        if (key == Key.System)
        {
            key = e.SystemKey;
        }

        // Pressing delete, backspace or escape without modifiers clears the current value
        if (modifiers == ModifierKeys.None &&
            (key == Key.Delete || key == Key.Back || key == Key.Escape))
        {
            Hotkey = null;
            return;
        }

        if (HotkeyUtils.IsKeyModifier(key) || !InputEnabled)
        {
            return;
        }

        InputEnabled = false;

        // Update the value if it's not spamming the key
        if (!e.IsRepeat)
        {
            Hotkey = new Hotkey(key, modifiers);
        }
        await Task.Delay(500);
        InputEnabled = true;
    }
}