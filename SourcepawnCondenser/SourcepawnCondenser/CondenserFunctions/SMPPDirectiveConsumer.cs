using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser
{
    public partial class Condenser
    {
        private int ConsumeSMPPDirective()
        {
            if (t[position].Value != "#define" || position + 1 >= length ||
                t[position + 1].Kind != TokenKind.Identifier)
            {
                return -1;
            }

            def.Defines.Add(new SMDefine
            {
                Index = t[position].Index,
                Length = t[position + 1].Index - t[position].Index + t[position + 1].Length,
                File = FileName,
                Name = t[position + 1].Value
            });
            
            for (var j = position + 1; j < length; ++j)
            {
                if (t[j].Kind == TokenKind.EOL)
                {
                    return j;
                }
            }

            return position + 1;
        }
    }
}