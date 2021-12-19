using System.Collections.Generic;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMEnumStruct : SMBaseDefinition
    {
        public List<SMEnumStructField> Fields = new();
        public List<SMEnumStructMethod> Methods = new();
    }

    public class SMEnumStructField : SMBaseDefinition
    {
        public string MethodmapName = string.Empty;
        public string FullName = string.Empty;
        //public string Type = string.Empty; not needed yet
    }

    public class SMEnumStructMethod : SMBaseDefinition
    {
        public string MethodmapName = string.Empty;
        public string FullName = string.Empty;
        public string ReturnType = string.Empty;
        public string[] Parameters = new string[0];
        public string[] MethodKind = new string[0];
    }
}