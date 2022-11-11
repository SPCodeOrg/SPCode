using System.Collections.Generic;
using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser;

public partial class Condenser
{
    private int ConsumeSMEnum()
    {
        var startIndex = _tokens[_position].Index;
        if ((_position + 1) < _length)
        {
            var iteratePosition = _position;
            var enumName = string.Empty;
            while ((iteratePosition + 1) < _length && _tokens[iteratePosition].Kind != TokenKind.BraceOpen)
            {
                if (_tokens[iteratePosition].Kind == TokenKind.Identifier)
                {
                    enumName = _tokens[iteratePosition].Value;
                }
                ++iteratePosition;
            }
            var braceState = 0;
            var inIgnoreMode = false;
            var endTokenIndex = -1;
            var entries = new List<string>();
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
                if (inIgnoreMode)
                {
                    if (_tokens[iteratePosition].Kind == TokenKind.Comma)
                    {
                        inIgnoreMode = false;
                    }
                    continue;
                }
                if (_tokens[iteratePosition].Kind == TokenKind.Identifier)
                {
                    entries.Add(_tokens[iteratePosition].Value);
                    inIgnoreMode = true;
                }
            }
            if (endTokenIndex == -1)
            {
                return -1;
            }
            _def.Enums.Add(new SMEnum()
            {
                Index = startIndex,
                Length = _tokens[endTokenIndex].Index - startIndex + 1,
                File = _fileName,
                Entries = entries.ToArray(),
                Name = enumName
            });
            return endTokenIndex;
        }
        return -1;
    }
}
