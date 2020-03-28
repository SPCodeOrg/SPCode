using System.Collections.Generic;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMEnumStruct
    {
        public int Index = -1;
        public int Length = 0;
        public string File = string.Empty;

        public string Name = string.Empty;
        
        public List<SMEnumStructField> Fields = new List<SMEnumStructField>();
        public List<SMEnumStructMethod> Methods = new List<SMEnumStructMethod>();
    }
    
    public class SMEnumStructField
    {
        public int Index = -1;
        public int Length = 0;
        public string File = string.Empty;

        public string Name = string.Empty;
        public string MethodmapName = string.Empty;
        public string FullName = string.Empty;
        //public string Type = string.Empty; not needed yet
    }

    public class SMEnumStructMethod
    {
        public int Index = -1;
        public int Length = 0;
        public string File = string.Empty;

        public string Name = string.Empty;
        public string MethodmapName = string.Empty;
        public string FullName = string.Empty;
        public string ReturnType = string.Empty;
        public string CommentString = string.Empty;
        public string[] Parameters = new string[0];
        public string[] MethodKind = new string[0];
    }
}