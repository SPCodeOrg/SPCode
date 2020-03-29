using System.Collections.Generic;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMVariable
    {
        public int Index = -1;
        public int Length = 0;
        public string File = string.Empty;
        public string Name = string.Empty;
        public string Type = string.Empty;
        public string Value = string.Empty;
        public int Dimensions = 0;
        public List<string> Size = new List<string>(); 
    }
}