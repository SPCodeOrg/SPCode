using System.Collections.Immutable;
using System.Linq;
using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser
{
    public class SPPPDirectiveConsumerNew : SourcePawnConsumer
    {
        public SPPPDirectiveConsumerNew(ImmutableList<Token> tokens, string fileName) : base(tokens, fileName)
        {
        }

        public override (SMBaseDefinition? def, int count) Consume(int position)
        {
            var tokens = Tokens.GetRange(position, Tokens.Count - position);
            if (tokens.Count < 2)
            {
                return (null, 1);
            }

            if (tokens.First().Value == "#define")
            {
                if (tokens[1].Kind == TokenKind.Identifier)
                {
                    var def = new SMDefine(tokens[0].Index, tokens[1].Index - tokens[0].Index + tokens[1].Length,
                        FileName, tokens[1].Value, "");

                    var eof = tokens.FindIndex((token) => token.Kind == TokenKind.EOF);
                    if (eof != -1)
                    {
                        return (def, eof);
                    }

                    return (def, 1);
                }
            }
            return (null, 1);
        }
    }
}