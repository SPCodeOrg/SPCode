using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser;

public partial class Condenser
{
    private int ConsumeSMTypedef()
    {
        var startIndex = _tokens[_position].Index;
        if ((_position + 2) < _length)
        {
            ++_position;
            if (_tokens[_position].Kind == TokenKind.Identifier)
            {
                var name = _tokens[_position].Value;
                for (var iteratePosition = _position + 1; iteratePosition < _length; ++iteratePosition)
                {
                    if (_tokens[iteratePosition].Kind == TokenKind.Semicolon)
                    {
                        _def.Typedefs.Add(new SMTypedef()
                        {
                            Index = startIndex,
                            Length = _tokens[iteratePosition].Index - startIndex + 1,
                            File = _fileName,
                            Name = name,
                            FullName = _source.Substring(startIndex, _tokens[iteratePosition].Index - startIndex + 1)
                        });
                        return iteratePosition;
                    }
                }
            }
        }
        return -1;
    }

    private int ConsumeSMTypeset()
    {
        var startIndex = _tokens[_position].Index;
        if ((_position + 2) < _length)
        {
            ++_position;
            if (_tokens[_position].Kind == TokenKind.Identifier)
            {
                var name = _tokens[_position].Value;
                var bracketIndex = 0;
                for (var iteratePosition = _position + 1; iteratePosition < _length; ++iteratePosition)
                {
                    if (_tokens[iteratePosition].Kind == TokenKind.BraceClose)
                    {
                        --bracketIndex;
                        if (bracketIndex == 0)
                        {
                            _def.Typedefs.Add(new SMTypedef()
                            {
                                Index = startIndex,
                                Length = _tokens[iteratePosition].Index - startIndex + 1,
                                File = _fileName,
                                Name = name,
                                FullName = _source.Substring(startIndex, _tokens[iteratePosition].Index - startIndex + 1)
                            });
                            return iteratePosition;
                        }
                    }
                    else if (_tokens[iteratePosition].Kind == TokenKind.BraceOpen)
                    {
                        ++bracketIndex;
                    }
                }
            }
        }
        return -1;
    }
}