using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser;

public partial class Condenser
{
    private int ConsumeSMStruct()
    {
        var startIndex = _tokens[_position].Index;
        if ((_position + 1) < _length)
        {
            var iteratePosition = _position;
            var structName = string.Empty;
            while ((iteratePosition + 1) < _length && _tokens[iteratePosition].Kind != TokenKind.BraceOpen)
            {
                if (_tokens[iteratePosition].Kind == TokenKind.Identifier)
                {
                    structName = _tokens[iteratePosition].Value;
                }
                ++iteratePosition;
            }
            var braceState = 0;
            var endTokenIndex = -1;
            for (; iteratePosition < _length; ++iteratePosition)
            {
                if (_tokens[iteratePosition].Kind == TokenKind.BraceOpen)
                {
                    ++braceState;
                    continue;
                }
                if (_tokens[iteratePosition].Kind == TokenKind.BraceClose)
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
            _def.Structs.Add(new SMStruct() { Index = startIndex, Length = _tokens[endTokenIndex].Index - startIndex + 1, File = _fileName, Name = structName });
            return endTokenIndex;
        }
        return -1;
    }
}