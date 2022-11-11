using System.Collections.Generic;
using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser;

public partial class Condenser
{
    private int ConsumeSMVariable()
    {
        if (_position + 3 < _length)
        {
            var startIndex = _tokens[_position].Index;

            var varType = _tokens[_position].Value;
            string varName;
            var size = new List<string>();
            var dimensions = 0;
            var index = -1;

            // Dynamic array: "char[] x = new char[64];"

            if (_tokens[_position + 1].Kind == TokenKind.Character)
            {
                for (var i = 1; i < _length; i += 2)
                {
                    index = i;
                    if (_tokens[_position + i].Kind == TokenKind.Identifier)
                    {
                        break;
                    }

                    if (_tokens[_position + i].Value == "[" && _tokens[_position + i + 1].Value == "]")
                    {
                        dimensions++;
                        continue;
                    }

                    return -1;
                }

                varName = _tokens[_position + index].Value;

                if (_tokens[_position + index + 1].Kind != TokenKind.Assignment ||
                    _tokens[_position + index + 2].Kind != TokenKind.New || _tokens[_position + index + 3].Value != varType)
                {
                    return -1;
                }

                for (var i = index + 4; i < _length - 2; i += 3)
                {
                    if (_tokens[_position + i].Kind == TokenKind.Semicolon)
                    {
                        _def.Variables.Add(new SMVariable
                        {
                            Index = startIndex,
                            Length = _tokens[_position + i].Index - startIndex,
                            File = _fileName,
                            Name = varName,
                            Type = varType,
                            Dimensions = dimensions,
                            Size = size,
                        });
                        return _position + i;
                    }

                    if (_tokens[_position + i].Value == "[" &&
                        (_tokens[_position + i + 1].Kind == TokenKind.Number ||
                         _tokens[_position + i + 1].Kind == TokenKind.Identifier) &&
                        _tokens[_position + i + 2].Value == "]")
                    {
                        size.Add(_tokens[_position + i + 1].Value);
                        continue;
                    }

                    return -1;
                }

                return -1;
            }

            if (_tokens[_position + 1].Kind != TokenKind.Identifier)
            {
                return -1;
            }

            varName = _tokens[_position + 1].Value;

            switch (_tokens[_position + 2].Kind)
            {
                // Simple var match: "int x;"
                case TokenKind.Semicolon:
                    _def.Variables.Add(new SMVariable
                    {
                        Index = startIndex,
                        Length = _tokens[_position + 2].Index - startIndex,
                        File = _fileName,
                        Name = varName,
                        Type = varType
                    });
                    return _position + 2;
                // Assign var match: "int x = 5"
                case TokenKind.Assignment when _tokens[_position + 4].Kind == TokenKind.Semicolon &&
                                               (_tokens[_position + 3].Kind == TokenKind.Number ||
                                                _tokens[_position + 3].Kind == TokenKind.Quote ||
                                                _tokens[_position + 3].Kind == TokenKind.Identifier):
                    _def.Variables.Add(new SMVariable
                    {
                        Index = startIndex,
                        Length = _tokens[_position + 4].Index - startIndex,
                        File = _fileName,
                        Name = varName,
                        Type = varType,
                        Value = _tokens[_position + 3].Value
                    });
                    return _position + 4;
            }


            // Array declaration match: "int x[3][3]...;"
            var requireAssign = false;

            for (var i = 2; i < _length;)
            {
                index = i;
                if (_tokens[_position + i].Kind == TokenKind.Semicolon)
                {
                    break;
                }

                if (_tokens[_position + i].Value == "[" && _tokens[_position + i + 2].Value == "]")
                {
                    if (_tokens[_position + 1].Kind != TokenKind.Number &&
                        _tokens[_position + 1].Kind != TokenKind.Identifier)
                    {
                        return -1;
                    }

                    dimensions++;
                    i += 3;
                    size.Add(_tokens[_position + i + 1].Value);
                    continue;
                }

                if (_tokens[_position + i].Value == "[" && _tokens[_position + i + 1].Value == "]")
                {
                    dimensions++;
                    i += 2;
                    requireAssign = true;
                    size.Add("-2");
                    continue;
                }

                break;
            }

            if (_tokens[_position + index].Kind == TokenKind.Semicolon && !requireAssign)
            {
                _def.Variables.Add(new SMVariable
                {
                    Index = startIndex,
                    Length = _tokens[_position + index].Index - startIndex,
                    File = _fileName,
                    Name = varName,
                    Type = varType,
                    Dimensions = dimensions,
                    Size = size
                });
                return _position + index;
            }

            // Array declaration with val "int x[3] = {1, 2, 3};
            if (_tokens[_position + index].Kind == TokenKind.Assignment)
            {
                // {1, 2, 3}
                if (_tokens[_position + index + 1].Kind == TokenKind.BraceOpen)
                {
                    var foundClose = false;
                    for (var i = index + 1; i < _length; i++)
                    {
                        index = i + 1;
                        if (_tokens[_position + i].Kind == TokenKind.BraceClose)
                        {
                            if (_tokens[_position + i + 1].Kind != TokenKind.Semicolon)
                            {
                                return -1;
                            }

                            foundClose = true;
                            break;
                        }
                    }

                    if (!foundClose)
                    {
                        return -1;
                    }

                    _def.Variables.Add(new SMVariable
                    {
                        Index = startIndex,
                        Length = _tokens[_position + index].Index - startIndex,
                        File = _fileName,
                        Name = varName,
                        Type = varType,
                        Dimensions = dimensions,
                        Size = size,
                        Value = "-1" //TODO: Add proper value
                    });
                }

                // "foobar"
                if (_tokens[_position + index + 1].Kind == TokenKind.Quote)
                {
                    if (_tokens[_position + index + 2].Kind != TokenKind.Semicolon)
                    {
                        return -1;
                    }

                    _def.Variables.Add(new SMVariable
                    {
                        Index = startIndex,
                        Length = _tokens[_position + index + 2].Index - startIndex,
                        File = _fileName,
                        Name = varName,
                        Type = varType,
                        Dimensions = dimensions,
                        Size = size,
                        Value = _tokens[_position + index + 1].Value
                    });
                }
            }
        }

        return -1;
    }
}