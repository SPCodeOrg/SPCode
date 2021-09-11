using System.Collections.Immutable;
using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser
{
    public abstract class SourcePawnConsumer
    {
        protected readonly ImmutableList<Token> Tokens;
        protected readonly string FileName;
        
        protected SourcePawnConsumer(ImmutableList<Token> tokens, string fileName)
        {
            Tokens = tokens;
            FileName = fileName;
        }

        public abstract (SMBaseDefinition? def, int count) Consume(int position);

    }
}