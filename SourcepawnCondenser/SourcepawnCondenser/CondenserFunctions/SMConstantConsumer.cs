using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser;

public partial class Condenser
{
    private int ConsumeSMConstant()
    {
        if (_position + 2 < _length)
        {
            var startIndex = _tokens[_position].Index;
            var foundIdentifier = false;
            var foundAssignment = false;
            var constantName = string.Empty;
            for (var i = _position + 2; i < _length; ++i)
            {
                if (_tokens[i].Kind == TokenKind.Semicolon)
                {
                    if (!foundIdentifier)
                    {
                        if (_tokens[i - 1].Kind == TokenKind.Identifier)
                        {
                            constantName = _tokens[i - 1].Value;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(constantName))
                    {
                        _def.ConstVariables.Add(new SMConstant
                        {
                            Index = startIndex,
                            Length = _tokens[i].Index - startIndex,
                            File = _fileName,
                            Name = constantName
                        });
                    }

                    return i;
                }

                if (_tokens[i].Kind == TokenKind.Assignment)
                {
                    foundAssignment = true;
                    if (_tokens[i - 1].Kind == TokenKind.Identifier)
                    {
                        foundIdentifier = true;
                        constantName = _tokens[i - 1].Value;
                    }
                }
                else if (_tokens[i].Kind == TokenKind.Character && !foundAssignment)
                {
                    if (_tokens[i].Value == "[")
                    {
                        if (_tokens[i - 1].Kind == TokenKind.Identifier)
                        {
                            foundIdentifier = true;
                            constantName = _tokens[i - 1].Value;
                        }
                    }
                }
                else if (_tokens[i].Kind == TokenKind.EOL) //failsafe
                {
                    return i;
                }
            }
        }
        return -1;
    }
}
