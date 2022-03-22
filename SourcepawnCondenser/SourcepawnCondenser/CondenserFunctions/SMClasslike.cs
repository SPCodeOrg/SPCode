using System;
using System.Collections.Generic;
using System.Linq;
using SourcepawnCondenser.SourcemodDefinition;

namespace SourcepawnCondenser;


/// <summary>
/// This is inherited from from SMMethodmap and SMEnumStruct
/// </summary>
public abstract class SMClasslike : SMBaseDefinition
{
    public readonly List<SMObjectField> Fields = new();
    public readonly List<SMObjectMethod> Methods = new();
    
    public virtual List<ACNode> ProduceNodes(SMDefinition smDef)
    {
        var nodes = new List<ACNode>();
        nodes.AddRange(ACNode.ConvertFromStringList(Methods.Select(e => e.Name), true, "▲ "));
        nodes.AddRange(ACNode.ConvertFromStringList(Fields.Select(e => e.Name), false, "• "));
            
        nodes.Sort((a, b) => string.CompareOrdinal(a.EntryName, b.EntryName));

        return nodes;
    }
}

public class SMObjectMethod : SMBaseDefinition
{
    public string ClassName = string.Empty;
    public string FullName = string.Empty;
    public string ReturnType = string.Empty;
    public string[] Parameters = Array.Empty<string>();
}

public class SMObjectField : SMBaseDefinition
{
    public string ClassName = string.Empty;
    public string FullName = string.Empty;
}