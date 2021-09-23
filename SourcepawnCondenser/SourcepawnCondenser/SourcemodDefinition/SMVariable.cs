using System.Collections.Generic;
using System.Collections.Immutable;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMVariable : SMBaseDefinition
    {
        public SMVariable(IImmutableList<Token> tokens, int endToken, string file, string name, string commentString) : base(tokens, endToken, file, name, commentString)
        {

        }
    }

    public class SMLocalVariable : SMVariable
    {
        public readonly IImmutableList<int> scopes;
        public SMLocalVariable(IImmutableList<Token> tokens, int endToken, string file, string name, string commentString, IImmutableList<int> scopes) : base(tokens, endToken, file, name, commentString)
        {
            this.scopes = scopes;
        }
    }
}
