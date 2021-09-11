using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser
{
    public class SPVariableConsumer : SourcePawnConsumer
    {
        public SPVariableConsumer(ImmutableList<Token> tokens, string fileName) : base(tokens, fileName)
        {
        }

        public override (SMBaseDefinition? def, int count) Consume(int position)
        {
            var tokens = Tokens.GetRange(position, Tokens.Count - position);
            if (tokens.Count < 3)
            {
                return (null, 1);
            }

            var startIndex = tokens.First().Index;
            
            string varName;
            var size = new List<string>();
            var dimensions = 0;
            var count = -1;

            // Dynamic array: "char[] x = new char[64];"

            if (tokens[1].Kind == TokenKind.Character)
            {
                for (var i = 1; i < tokens.Count - 1; i += 2)
                {
                    count = i;
                    if (tokens[i].Kind == TokenKind.Identifier)
                    {
                        break;
                    }

                    if (tokens[i].Value == "[" && tokens[i + 1].Value == "]")
                    {
                        dimensions++;
                        continue;
                    }

                    return (null, 1);
                }

                varName = tokens[count].Value;

                if (tokens[count + 1].Kind != TokenKind.Assignment ||
                    tokens[count + 2].Kind != TokenKind.New ||
                    tokens[count + 3].Kind != TokenKind.Identifier)
                {
                    return (null, 1);
                }

                for (var i = count + 4; i < tokens.Count - 2; i += 3)
                {
                    if (tokens[i].Kind == TokenKind.Semicolon)
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
                        return (
                                new SMVariable(startIndex, tokens[i].Index - startIndex, FileName, varName, varType, )
                                );
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


            return -1;
        }
    }
}