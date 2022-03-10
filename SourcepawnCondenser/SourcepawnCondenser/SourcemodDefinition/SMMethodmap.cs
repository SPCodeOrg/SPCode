using System.Collections.Generic;
using System.Linq;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMMethodmap : SMClasslike
    {
        public string InheritedType = string.Empty;


        public List<ACNode> ProduceNodes()
        {
            var nodes = new List<ACNode>();
            nodes.AddRange(ACNode.ConvertFromStringList(Methods.Select(e => e.Name), true, "▲ "));
            nodes.AddRange(ACNode.ConvertFromStringList(Fields.Select(e => e.Name), false, "• "));
            
            nodes.Sort((a, b) => string.CompareOrdinal(a.EntryName, b.EntryName));

            return nodes;
        }
    }

    public class SMMethodmapField : SMObjectField
    {
    }

    public class SMMethodmapMethod : SMObjectMethod
    {
    }
}