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
        public readonly List<string> Parameters;
        public readonly SMFunctionKind FunctionKind;
        public readonly List<SMVariable> FuncVariables;

        public SMFunction(IImmutableList<Token> tokens, int endToken, string file, string name, string commentString, string fullName, string returnType, List<string> parameters, SMFunctionKind functionKind, List<SMVariable> funcVariables) : base(tokens.First().Index, tokens[endToken].Index - tokens.First().Index, file, name, commentString)
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
