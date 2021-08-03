using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPCode.Utils
{
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
}
