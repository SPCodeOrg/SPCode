using System.Collections.Generic;
using System.Linq;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMMethodmap : SMBaseDefinition
    {
        public string Type = string.Empty;
        public string InheritedType = string.Empty;
        public readonly List<SMMethodmapField> Fields = new();
        public readonly List<SMMethodmapMethod> Methods = new();
        
        public List<ISNode> ProduceISNodes()
        {
            var nodes = new List<ISNode>();
            nodes.AddRange(ISNode.ConvertFromStringList(Methods.Select(e => e.Name), true, "▲ "));
            nodes.AddRange(ISNode.ConvertFromStringList(Fields.Select(e => e.Name), false, "• "));

            // nodes.AddRange(ISNode.ConvertFromStringArray(VariableStrings, false, "v "));

            nodes.Sort((a, b) => string.CompareOrdinal(a.EntryName, b.EntryName));

            return nodes;
        }
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