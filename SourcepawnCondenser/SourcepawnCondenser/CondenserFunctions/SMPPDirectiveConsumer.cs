using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser;

public partial class Condenser
{
    private int ConsumeSMPPDirective()
    {
        if (_tokens[_position].Value != "#define" || _position + 1 >= _length ||
            _tokens[_position + 1].Kind != TokenKind.Identifier)
        {
            return -1;
        }

        _def.Defines.Add(new SMDefine
        {
            Index = _tokens[_position].Index,
            Length = _tokens[_position + 1].Index - _tokens[_position].Index + _tokens[_position + 1].Length,
            File = _fileName,
            Name = _tokens[_position + 1].Value
        });
        
        for (var j = _position + 1; j < _length; ++j)
        {
            if (_tokens[j].Kind == TokenKind.EOL)
            {
                return j;
            }
        }

        return _position + 1;
    }
}