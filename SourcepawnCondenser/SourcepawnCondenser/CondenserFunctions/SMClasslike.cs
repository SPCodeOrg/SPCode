using System.Collections.Generic;
using SourcepawnCondenser.SourcemodDefinition;

namespace SourcepawnCondenser;


/// <summary>
/// This is inherited from from SMMethodmap and SMEnumStruct
/// </summary>
public abstract class SMClasslike : SMBaseDefinition
{
    public List<SMObjectField> Fields = new();
    public List<SMObjectMethod> Methods = new();
}

public abstract class SMObjectMethod : SMBaseDefinition
{
    public string ClassName = string.Empty;
    public string FullName = string.Empty;
    public string ReturnType = string.Empty;
}

public abstract class SMObjectField : SMBaseDefinition
{
    public string ClassName = string.Empty;
    public string FullName = string.Empty;
}