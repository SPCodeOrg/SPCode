using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMFunction : SMBaseDefinition
    {
        public int EndPos;

        public readonly string FullName;
        public readonly string ReturnType;
        public readonly IImmutableList<string> Parameters;
        public readonly SMFunctionKind FunctionKind;
        public readonly IImmutableList<SMVariable> FuncVariables;

        public SMFunction(IImmutableList<Token> tokens, int endToken, string file, string name, string commentString, string fullName, string returnType, IImmutableList<string> parameters, SMFunctionKind functionKind, IImmutableList<SMVariable> funcVariables) : base(tokens, endToken, file, name, commentString)
        {
            FullName = fullName;
            ReturnType = returnType;
            Parameters = parameters;
            FunctionKind = functionKind;
            FuncVariables = funcVariables;
        }
    }

    public enum SMFunctionKind
    {
        Stock,
        StockStatic,
        Native,
        Forward,
        Public,
        PublicNative,
        Static,
        Normal,
        None
    }
}
