using System.Collections.Generic;
using System.Collections.Immutable;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMMethodmap : SMBaseDefinition
    {
        public readonly string Type;
        public readonly string InheritedType;
        public readonly IImmutableList<SMMethodmapField> Fields;
        public readonly IImmutableList<SMMethodmapMethod> Methods;


        public SMMethodmap(IImmutableList<Token> tokens, int endToken, string file, string name, string commentString, string type, string inheritedType, IImmutableList<SMMethodmapField> fields, IImmutableList<SMMethodmapMethod> methods) : base(tokens, endToken, file, name, commentString)
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

        public SMMethodmapField(IImmutableList<Token> tokens, int endToken, string file, string name, string commentString, string methodmapName, string fullName) : base(tokens, endToken, file, name, commentString)
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
        public readonly IImmutableList<string> Parameters;
        public readonly IImmutableList<string> MethodKind;


        public SMMethodmapMethod(IImmutableList<Token> tokens, int endToken, string file, string name, string commentString, string methodmapName, string fullName, string returnType, IImmutableList<string> parameters, IImmutableList<string> methodKind) : base(tokens, endToken, file, name, commentString)
        {
            MethodmapName = methodmapName;
            FullName = fullName;
            ReturnType = returnType;
            Parameters = parameters;
            MethodKind = methodKind;
        }
    }
}
