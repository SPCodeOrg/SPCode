using System.Collections.Generic;
using System.Collections.Immutable;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMEnumStruct : SMBaseDefinition
    {
        public readonly IImmutableList<SMEnumStructField> Fields;
        public readonly IImmutableList<SMEnumStructMethod> Methods;


        public SMEnumStruct(IImmutableList<Token> tokens, int endToken, string file, string name, string commentString,
            IImmutableList<SMEnumStructField> fields, IImmutableList<SMEnumStructMethod> methods) : base(tokens, endToken,
            file, name, commentString)
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

        public SMEnumStructField(IImmutableList<Token> tokens, int endToken, string file, string name,
            string commentString, string methodmapName, string fullName) : base(tokens, endToken, file, name,
            commentString)
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
        public readonly IImmutableList<string> Parameters;
        public readonly IImmutableList<string> MethodKind;


        public SMEnumStructMethod(IImmutableList<Token> tokens, int endToken, string file, string name,
            string commentString, string methodmapName, string fullName, string returnType,
            IImmutableList<string> parameters, IImmutableList<string> methodKind) : base(tokens, endToken, file, name,
            commentString)
        {
            MethodmapName = methodmapName;
            FullName = fullName;
            ReturnType = returnType;
            Parameters = parameters;
            MethodKind = methodKind;
        }
    }
}