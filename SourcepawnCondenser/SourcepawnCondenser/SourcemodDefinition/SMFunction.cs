using System.Collections.Generic;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMFunction : SMBaseDefinition
    {
        public int EndPos = -1;

        public string FullName = string.Empty;
        public string ReturnType = string.Empty;
        public string[] Parameters = new string[0];
        public SMFunctionKind FunctionKind = SMFunctionKind.Unknown;
        public List<SMVariable> FuncVariables = new List<SMVariable>();
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
        Unknown
    }
}
