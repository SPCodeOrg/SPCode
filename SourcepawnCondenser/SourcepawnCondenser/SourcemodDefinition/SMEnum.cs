using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser.SourcemodDefinition
{
    public class SMEnum : SMBaseDefinition
    {
        public readonly IImmutableList<string> Entries;


        public SMEnum(IImmutableList<Token> tokens, int endToken, string file, string name, string commentString, IImmutableList<string> entries) : base(tokens, endToken, file, name, commentString)
        {
            Entries = entries;
        }
    }
}
