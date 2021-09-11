using System.Collections.Generic;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMMethodmap : SMBaseDefinition
    {
        public readonly string Type;
        public readonly string InheritedType;
        public readonly List<SMMethodmapField> Fields;
        public readonly List<SMMethodmapMethod> Methods;

        public SMMethodmap(int index, int length, string file, string name, string commentString, string type, string inheritedType, List<SMMethodmapField> fields, List<SMMethodmapMethod> methods) : base(index, length, file, name, commentString)
        {
            Type = type;
            InheritedType = inheritedType;
            Fields = fields;
            Methods = methods;
        }
    }

    public class SMMethodmapField : SMBaseDefinition
    {
        public readonly string MethodmapName;
        public readonly string FullName;
        //public string Type = string.Empty; not needed yet
        public SMMethodmapField(int index, int length, string file, string name, string commentString, string methodmapName, string fullName) : base(index, length, file, name, commentString)
        {
            MethodmapName = methodmapName;
            FullName = fullName;
        }
    }

    public class SMMethodmapMethod : SMBaseDefinition
    {
        public readonly string MethodmapName;
        public readonly string FullName;
        public readonly string ReturnType;
        public readonly List<string> Parameters;
        public readonly List<string> MethodKind;

        public SMMethodmapMethod(int index, int length, string file, string name, string commentString, string methodmapName, string fullName, string returnType, List<string> parameters, List<string> methodKind) : base(index, length, file, name, commentString)
        {
            MethodmapName = methodmapName;
            FullName = fullName;
            ReturnType = returnType;
            Parameters = parameters;
            MethodKind = methodKind;
        }
    }
}
