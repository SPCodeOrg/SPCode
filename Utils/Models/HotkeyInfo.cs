namespace SPCode.Utils;

public class HotkeyInfo
{
    public Hotkey Hotkey { get; set; }
    public string Command { get; set; }

    public HotkeyInfo(Hotkey hk, string command)
    {
        Hotkey = hk;
        Command = command;
    }
}