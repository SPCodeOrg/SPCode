using System.Collections.Immutable;
using System.Dynamic;
using System.Linq;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public abstract class SMBaseDefinition
    {
        protected SMBaseDefinition(IImmutableList<Token> tokens, int endToken, string file, string name, string commentString)
        {
            Index = tokens.First().Length;
            Length = tokens[endToken].Index - tokens.First().Index + tokens[endToken].Length;
            File = file;
            Name = name;
            CommentString = commentString;
        }

        public int Index { get; }
        public int Length { get; }
        public string File { get; }
        public string Name { get; }
        public string CommentString { get; }
    }
}