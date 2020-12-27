using System.Collections.Generic;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMVariable : SMBaseDefinition
    {
        public string Type = string.Empty;
        public string Value = string.Empty;
        public int Dimensions = 0;
        public List<string> Size = new List<string>();
    }
}