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
                return InvalidValue;
            }

            if (tokens.First().Value != "#define")
            {
                return InvalidValue;
            }

            if (tokens[1].Kind != TokenKind.Identifier)
            {
                return InvalidValue;
            }

            var def = new SMDefine(tokens, 1,
                FileName, tokens[1].Value, "");

            var eof = tokens.FindIndex(token => token.Kind == TokenKind.EOF);
            return eof != -1 ? (def, eof) : (def, 1);
        }
    }
}