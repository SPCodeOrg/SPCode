using System.Windows.Input;

namespace SPCode.Utils;

public class HotkeyUtils
{
    public static bool IsKeyModifier(Key key)
    {
        return key is
            Key.LeftCtrl or
            Key.RightCtrl or
            Key.LeftAlt or
            Key.RightAlt or
            Key.LeftShift or
            Key.RightShift or
            Key.LWin or
            Key.RWin or
            Key.Clear or
            Key.OemClear or
            Key.Apps;
    }
}