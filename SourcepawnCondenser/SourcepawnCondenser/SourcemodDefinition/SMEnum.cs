using System;
using System.Collections.Generic;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMEnum : SMBaseDefinition
    {
        public List<string> Entries;

        public SMEnum(int index, int length, string file, string name, string commentString, List<string> entries) : base(index, length, file, name, commentString)
        {
            Entries = entries;
        }
    }
}
