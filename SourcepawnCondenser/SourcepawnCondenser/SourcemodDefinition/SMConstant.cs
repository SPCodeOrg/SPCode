using System.Collections.Immutable;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMConstant : SMBaseDefinition
    {
        public SMConstant(IImmutableList<Token> tokens, int endToken, string file, string name, string commentString) : base(tokens, endToken, file, name, commentString)
        {
        }
    }
}