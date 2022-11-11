using System.Collections.Generic;
using System.Linq;

namespace SourcepawnCondenser.SourcemodDefinition;

public class SMMethodmap : SMClasslike
{
    public string InheritedType = string.Empty;

    public override List<ACNode> ProduceNodes(SMDefinition smDef)
    {
        var nodes = new List<ACNode>();
        nodes.AddRange(ACNode.ConvertFromStringList(Methods.Select(e => e.Name), true, "▲ "));
        nodes.AddRange(ACNode.ConvertFromStringList(Fields.Select(e => e.Name), false, "• "));

        // Find Fields and Methods inherited from the "super-class".
        var inheritedType = InheritedType;
        for (;;)
        {
            if (inheritedType.Length == 0)
                break;

            var mm = smDef.Methodmaps.Find(e => e.Name == inheritedType);
            if (mm == null)
                break;

            nodes.AddRange(ACNode.ConvertFromStringList(mm.Methods.Select(e => e.Name), true, "▲ "));
            nodes.AddRange(ACNode.ConvertFromStringList(mm.Fields.Select(e => e.Name), false, "• "));

            inheritedType = mm.InheritedType;
        }

        nodes.Sort((a, b) => string.CompareOrdinal(a.EntryName, b.EntryName));

        return nodes;
    }
}