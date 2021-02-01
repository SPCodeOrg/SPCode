using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser
{
    public partial class Condenser
    {
        private int ConsumeSMStruct()
        {
            var startIndex = t[position].Index;
            if ((position + 1) < length)
            {
                var iteratePosition = position;
                var structName = string.Empty;
                while ((iteratePosition + 1) < length && t[iteratePosition].Kind != TokenKind.BraceOpen)
                {
                    if (t[iteratePosition].Kind == TokenKind.Identifier)
                    {
                        structName = t[iteratePosition].Value;
                    }
                    ++iteratePosition;
                }
                var braceState = 0;
                var endTokenIndex = -1;
                for (; iteratePosition < length; ++iteratePosition)
                {
                    if (t[iteratePosition].Kind == TokenKind.BraceOpen)
                    {
                        ++braceState;
                        continue;
                    }
                    if (t[iteratePosition].Kind == TokenKind.BraceClose)
                    {
                        --braceState;
                        if (braceState == 0)
                        {
                            endTokenIndex = iteratePosition;
                            break;
                        }
                        continue;
                    }
                }
                if (endTokenIndex == -1)
                {
                    return -1;
                }
                def.Structs.Add(new SMStruct() { Index = startIndex, Length = t[endTokenIndex].Index - startIndex + 1, File = FileName, Name = structName });
                return endTokenIndex;
            }
            return -1;
        }
    }
}
