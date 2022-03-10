using System.Collections.Generic;
using System.Linq;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMEnumStruct : SMBaseDefinition
    {
        public readonly List<SMEnumStructField> Fields = new();
        public readonly List<SMEnumStructMethod> Methods = new();
        
        
        public List<ACNode> ProduceNodes()
        {
            var nodes = new List<ACNode>();
            nodes.AddRange(ACNode.ConvertFromStringList(Methods.Select(e => e.Name), true, "▲ "));
            nodes.AddRange(ACNode.ConvertFromStringList(Fields.Select(e => e.Name), false, "• "));
            
            nodes.Sort((a, b) => string.CompareOrdinal(a.EntryName, b.EntryName));

            return nodes;
        }
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