using System.Collections.Generic;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMMethodmap : SMBaseDefinition
    {
        public string Type = string.Empty;
        public string InheritedType = string.Empty;
        public List<SMMethodmapField> Fields = new();
        public List<SMMethodmapMethod> Methods = new();
    }

    public class SMMethodmapField : SMBaseDefinition
    {
        public string MethodmapName = string.Empty;
        public string FullName = string.Empty;
        //public string Type = string.Empty; not needed yet
    }

    public class SMMethodmapMethod : SMBaseDefinition
    {
        public string MethodmapName = string.Empty;
        public string FullName = string.Empty;
        public string ReturnType = string.Empty;
        public string[] Parameters = new string[0];
        public string[] MethodKind = new string[0];
    }
}