using System.Collections.Generic;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMEnumStruct : SMBaseDefinition
    {
        public readonly List<SMEnumStructField> Fields;
        public readonly List<SMEnumStructMethod> Methods;

        public SMEnumStruct(int index, int length, string file, string name, string commentString, List<SMEnumStructField> fields, List<SMEnumStructMethod> methods) : base(index, length, file, name, commentString)
        {
            Fields = fields;
            Methods = methods;
        }
    }

    public class SMEnumStructField : SMBaseDefinition
    {
        public readonly string MethodmapName;
        public readonly string FullName;
        //public string Type = string.Empty; not needed yet
        public SMEnumStructField(int index, int length, string file, string name, string commentString, string methodmapName, string fullName) : base(index, length, file, name, commentString)
        {
            MethodmapName = methodmapName;
            FullName = fullName;
        }
    }

    public class SMEnumStructMethod : SMBaseDefinition
    {
        public readonly string MethodmapName;
        public readonly string FullName;
        public readonly string ReturnType;
        public readonly List<string> Parameters;
        public readonly List<string> MethodKind;

        public SMEnumStructMethod(int index, int length, string file, string name, string commentString, string methodmapName, string fullName, string returnType, List<string> parameters, List<string> methodKind) : base(index, length, file, name, commentString)
        {
            MethodmapName = methodmapName;
            FullName = fullName;
            ReturnType = returnType;
            Parameters = parameters;
            MethodKind = methodKind;
        }
    }
}