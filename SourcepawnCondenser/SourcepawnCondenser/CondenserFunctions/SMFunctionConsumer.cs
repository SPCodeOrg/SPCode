using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser
{
    public partial class Condenser
    {
        private int ConsumeSMFunction()
        {
            var kind = SMFunctionKind.Unknown;
            var startPosition = _position;
            var iteratePosition = startPosition + 1;
            var functionReturnType = string.Empty;
            var functionName = string.Empty;

            switch (_tokens[startPosition].Value)
            {
                case "stock":
                {
                    if (startPosition + 1 < _length)
                    {
                        if (_tokens[startPosition + 1].Kind == TokenKind.FunctionIndicator)
                        {
                            if (_tokens[startPosition + 1].Value == "static")
                            {
                                kind = SMFunctionKind.StockStatic;
                                ++iteratePosition;
                                break;
                            }
                        }
                    }

                    kind = SMFunctionKind.Stock;
                    break;
                }
                case "native":
                {
                    kind = SMFunctionKind.Native;
                    break;
                }
                case "forward":
                {
                    kind = SMFunctionKind.Forward;
                    break;
                }
                case "public":
                {
                    if (startPosition + 1 < _length)
                    {
                        if (_tokens[startPosition + 1].Kind == TokenKind.FunctionIndicator)
                        {
                            if (_tokens[startPosition + 1].Value == "native")
                            {
                                kind = SMFunctionKind.PublicNative;
                                ++iteratePosition;
                                break;
                            }
                        }
                    }

                    kind = SMFunctionKind.Public;
                    break;
                }
                case "static":
                {
                    kind = SMFunctionKind.Static;
                    break;
                }
                case "normal":
                {
                    kind = SMFunctionKind.Normal;
                    break;
                }
                default:
                {
                    functionReturnType = _tokens[startPosition].Value;
                    break;
                }
            }

            var functionCommentString = string.Empty;
            var commentTokenIndex = BacktraceTestForToken(startPosition - 1, TokenKind.MultiLineComment, true, false);
            if (commentTokenIndex == -1)
            {
                commentTokenIndex = BacktraceTestForToken(startPosition - 1, TokenKind.SingleLineComment, true, false);
                if (commentTokenIndex != -1)
                {
                    var strBuilder = new StringBuilder(_tokens[commentTokenIndex].Value);
                    while ((commentTokenIndex =
                        BacktraceTestForToken(commentTokenIndex - 1, TokenKind.SingleLineComment, true,
                            false)) != -1)
                    {
                        strBuilder.Insert(0, Environment.NewLine);
                        strBuilder.Insert(0, _tokens[commentTokenIndex].Value);
                    }

                    functionCommentString = strBuilder.ToString();
                }
            }
            else
            {
                functionCommentString = _tokens[commentTokenIndex].Value;
            }

            for (; iteratePosition < startPosition + 5; ++iteratePosition)
            {
                if (_tokens.Length > iteratePosition + 1)
                {
                    if (_tokens[iteratePosition].Kind == TokenKind.Identifier)
                    {
                        if (_tokens[iteratePosition + 1].Kind == TokenKind.ParenthesisOpen)
                        {
                            functionName = _tokens[iteratePosition].Value;
                            break;
                        }

                        functionReturnType = _tokens[iteratePosition].Value;
                        continue;
                    }

                    if (_tokens[iteratePosition].Kind == TokenKind.Character)
                    {
                        if (_tokens[iteratePosition].Value.Length > 0)
                        {
                            var testChar = _tokens[iteratePosition].Value[0];
                            if (testChar is ':' or '[' or ']')
                            {
                                continue;
                            }
                        }
                    }

                    return -1;
                }

                return -1;
            }

            if (string.IsNullOrEmpty(functionName))
            {
                return -1;
            }

            ++iteratePosition;
            var functionParameters = new List<string>();
            var parameterDeclIndexStart = _tokens[iteratePosition].Index;
            var parameterDeclIndexEnd = -1;
            var lastParameterIndex = parameterDeclIndexStart;
            var parenthesisCounter = 0;
            var gotCommaBreak = false;
            var outTokenIndex = -1;
            var braceState = 0;
            for (; iteratePosition < _length; ++iteratePosition)
            {
                if (_tokens[iteratePosition].Kind == TokenKind.ParenthesisOpen)
                {
                    ++parenthesisCounter;
                    continue;
                }

                if (_tokens[iteratePosition].Kind == TokenKind.ParenthesisClose)
                {
                    --parenthesisCounter;
                    if (parenthesisCounter == 0)
                    {
                        outTokenIndex = iteratePosition;
                        parameterDeclIndexEnd = _tokens[iteratePosition].Index;
                        var length = _tokens[iteratePosition].Index - 1 - (lastParameterIndex + 1);
                        if (gotCommaBreak)
                        {
                            functionParameters.Add(length == 0
                                ? string.Empty
                                : _source.Substring(lastParameterIndex + 1, length + 1).Trim());
                        }
                        else if (length > 0)
                        {
                            var singleParameterString = _source.Substring(lastParameterIndex + 1, length + 1);
                            if (!string.IsNullOrWhiteSpace(singleParameterString))
                            {
                                functionParameters.Add(singleParameterString);
                            }
                        }

                        break;
                    }

                    continue;
                }

                if (_tokens[iteratePosition].Kind == TokenKind.BraceOpen)
                {
                    ++braceState;
                }

                if (_tokens[iteratePosition].Kind == TokenKind.BraceClose)
                {
                    --braceState;
                }

                if (_tokens[iteratePosition].Kind == TokenKind.Comma && braceState == 0)
                {
                    gotCommaBreak = true;
                    var length = _tokens[iteratePosition].Index - 1 - (lastParameterIndex + 1);
                    functionParameters.Add(length == 0
                        ? string.Empty
                        : _source.Substring(lastParameterIndex + 1, length + 1).Trim());
                    lastParameterIndex = _tokens[iteratePosition].Index;
                }
            }

            if (parameterDeclIndexEnd == -1)
            {
                return -1;
            }

            var localVars = new List<SMVariable>();

            if (outTokenIndex + 1 < _length)
            {
                if (_tokens[outTokenIndex + 1].Kind == TokenKind.Semicolon)
                {
                    _def.Functions.Add(new SMFunction
                    {
                        FunctionKind = kind,
                        Index = _tokens[startPosition].Index,
                        EndPos = parameterDeclIndexEnd,
                        File = _fileName,
                        Length = parameterDeclIndexEnd - _tokens[startPosition].Index + 1,
                        Name = functionName,
                        FullName = TrimFullname(_source.Substring(_tokens[startPosition].Index,
                            parameterDeclIndexEnd - _tokens[startPosition].Index + 1)),
                        ReturnType = functionReturnType,
                        CommentString = TrimComments(functionCommentString),
                        Parameters = functionParameters.ToArray(),
                        FuncVariables = localVars
                    });
                    return outTokenIndex + 1;
                }
                var nextOpenBraceTokenIndex = FortraceTestForToken(outTokenIndex + 1, TokenKind.BraceOpen, true, false);
                if (nextOpenBraceTokenIndex != -1)
                {
                    braceState = 0;
                    for (var i = nextOpenBraceTokenIndex; i < _length; ++i)
                    {
                        if (_tokens[i].Kind == TokenKind.BraceOpen)
                        {
                            ++braceState;
                        }
                        else if (_tokens[i].Kind == TokenKind.BraceClose)
                        {
                            --braceState;
                            if (braceState == 0)
                            {
                                var segment = new ArraySegment<Token>(_tokens, nextOpenBraceTokenIndex,
                                    i - nextOpenBraceTokenIndex);
                                localVars = LocalVars.ConsumeSMVariableLocal(segment.ToArray(), _fileName);
                                _def.Functions.Add(new SMFunction
                                {
                                    EndPos = _tokens[nextOpenBraceTokenIndex + (i - nextOpenBraceTokenIndex) + 1].Index,
                                    FunctionKind = kind,
                                    Index = _tokens[startPosition].Index,
                                    File = _fileName,
                                    Length = parameterDeclIndexEnd - _tokens[startPosition].Index + 1,
                                    Name = functionName,
                                    FullName = TrimFullname(_source.Substring(_tokens[startPosition].Index,
                                        parameterDeclIndexEnd - _tokens[startPosition].Index + 1)),
                                    ReturnType = functionReturnType,
                                    CommentString = TrimComments(functionCommentString),
                                    Parameters = functionParameters.ToArray(),
                                    FuncVariables = localVars
                                });
                                return i;
                            }
                        }
                    }
                }
            }

            _def.Functions.Add(new SMFunction
            {
                EndPos = parameterDeclIndexEnd,
                FunctionKind = kind,
                Index = _tokens[startPosition].Index,
                File = _fileName,
                Length = parameterDeclIndexEnd - _tokens[startPosition].Index + 1,
                Name = functionName,
                FullName = TrimFullname(_source.Substring(_tokens[startPosition].Index,
                    parameterDeclIndexEnd - _tokens[startPosition].Index + 1)),
                ReturnType = functionReturnType,
                CommentString = TrimComments(functionCommentString),
                Parameters = functionParameters.ToArray(),
                FuncVariables = localVars
            });
            return outTokenIndex;
        }
    }

    internal static class LocalVars
    {
        public static List<SMVariable> ConsumeSMVariableLocal(Token[] t, string FileName)
        {
            var position = 0;
            var length = t.Length;

            var variables = new List<SMVariable>();
            if (3 >= length)
            {
                return variables;
            }
            try
            {
                while (position < length)
                {
                    if (t[position].Kind != TokenKind.Identifier)
                    {
                        position++;
                        continue;
                    }
                    var startIndex = t[position].Index;

                    var varType = t[position].Value;
                    string varName;
                    var size = new List<string>();
                    var dimensions = 0;
                    var index = -1;

                    // Dynamic array: "char[] x = new char[64];"
                    var continueNext = false;

                    if (position + 3 >= length)
                    {
                        return variables;
                    }

                    if (t[position + 1].Kind == TokenKind.Character)
                    {
                        for (var i = 1; i < length; i += 2)
                        {
                            index = i;
                            if (t[position + i].Kind == TokenKind.Identifier)
                            {
                                break;
                            }

                            if (t[position + i].Value == "[" && t[position + i + 1].Value == "]")
                            {
                                dimensions++;
                                continue;
                            }

                            continueNext = true;
                            break;
                        }

                        if (continueNext)
                        {
                            position++;
                            continue;
                        }


                        varName = t[position + index].Value;

                        if (t[position + index + 1].Kind != TokenKind.Assignment ||
                            t[position + index + 2].Kind != TokenKind.New || t[position + index + 3].Value != varType)
                        {
                            position++;
                            continue;
                        }

                        for (var i = index + 4; i < length - 2; i += 3)
                        {
                            if (t[position + i].Kind == TokenKind.Semicolon)
                            {
                                variables.Add(new SMVariable
                                {
                                    Index = startIndex,
                                    Length = t[position + i].Index - startIndex,
                                    File = FileName,
                                    Name = varName,
                                    Type = varType,
                                    Dimensions = dimensions,
                                    Size = size
                                });
                                continueNext = true;
                                position += i;
                                break;
                            }

                            if (t[position + i].Value == "[" &&
                                (t[position + i + 1].Kind == TokenKind.Number ||
                                 t[position + i + 1].Kind == TokenKind.Identifier) &&
                                t[position + i + 2].Value == "]")
                            {
                                size.Add(t[position + i + 1].Value);
                                continue;
                            }

                            continueNext = true;
                            position++;
                            break;
                        }

                        if (continueNext)
                        {
                            continue;
                        }
                    }

                    if (t[position + 1].Kind != TokenKind.Identifier)
                    {
                        position++;
                        continue;
                    }

                    varName = t[position + 1].Value;

                    switch (t[position + 2].Kind)
                    {
                        // Simple var match: "int x;"
                        case TokenKind.Semicolon:
                            variables.Add(new SMVariable
                            {
                                Index = startIndex,
                                Length = t[position + 2].Index - startIndex,
                                File = FileName,
                                Name = varName,
                                Type = varType
                            });
                            position += 2;
                            continueNext = true;
                            break;
                        // Assign var match: "int x = 5"
                        case TokenKind.Assignment when t[position + 3].Kind == TokenKind.Number ||
                                                        t[position + 3].Kind == TokenKind.Quote ||
                                                        t[position + 3].Kind == TokenKind.Identifier ||
                                                        t[position + 3].Kind == TokenKind.New:
                            variables.Add(new SMVariable
                            {
                                Index = startIndex,
                                Length = t[position + 4].Index - startIndex,
                                File = FileName,
                                Name = varName,
                                Type = varType,
                                Value = t[position + 3].Value
                            });
                            position += 4;
                            continueNext = true;
                            break;
                    }

                    if (continueNext)
                    {
                        continue;
                    }

                    // Array declaration match: "int x[3][3]...;"
                    var requireAssign = false;

                    for (var i = 2; i < length;)
                    {
                        index = i;
                        if (t[position + i].Kind == TokenKind.Semicolon)
                        {
                            break;
                        }

                        if (t[position + i].Value == "[" && t[position + i + 2].Value == "]")
                        {
                            if (t[position + 1].Kind != TokenKind.Number &&
                                t[position + 1].Kind != TokenKind.Identifier)
                            {
                                continueNext = true;
                                position++;
                                break;
                            }

                            dimensions++;
                            i += 3;
                            size.Add(t[position + i + 1].Value);
                            continue;
                        }

                        if (t[position + i].Value == "[" && t[position + i + 1].Value == "]")
                        {
                            dimensions++;
                            i += 2;
                            requireAssign = true;
                            size.Add("-2");
                            continue;
                        }

                        break;
                    }

                    if (continueNext)
                    {
                        continue;
                    }

                    if (t[position + index].Kind == TokenKind.Semicolon && !requireAssign)
                    {
                        variables.Add(new SMVariable
                        {
                            Index = startIndex,
                            Length = t[position + index].Index - startIndex,
                            File = FileName,
                            Name = varName,
                            Type = varType,
                            Dimensions = dimensions,
                            Size = size
                        });
                        position += index;
                        continue;
                    }

                    // Array declaration with val "int x[3] = {1, 2, 3};
                    if (t[position + index].Kind == TokenKind.Assignment)
                    {
                        // {1, 2, 3}
                        if (t[position + index + 1].Kind == TokenKind.BraceOpen)
                        {
                            var foundClose = false;
                            for (var i = index + 1; i < length; i++)
                            {
                                index = i + 1;
                                if (t[position + i].Kind == TokenKind.BraceClose)
                                {
                                    if (t[position + i + 1].Kind != TokenKind.Semicolon)
                                    {
                                        continueNext = true;
                                        break;
                                    }

                                    foundClose = true;
                                    break;
                                }
                            }

                            if (!foundClose || continueNext)
                            {
                                position++;
                                continue;
                            }

                            variables.Add(new SMVariable
                            {
                                Index = startIndex,
                                Length = t[position + index].Index - startIndex,
                                File = FileName,
                                Name = varName,
                                Type = varType,
                                Dimensions = dimensions,
                                Size = size,
                                Value = "-1" //TODO: Add proper value
                            });
                        }

                        // "foobar"
                        if (t[position + index + 1].Kind == TokenKind.Quote)
                        {
                            if (t[position + index + 2].Kind != TokenKind.Semicolon)
                            {
                                position++;
                                continue;
                            }

                            variables.Add(new SMVariable
                            {
                                Index = startIndex,
                                Length = t[position + index + 2].Index - startIndex,
                                File = FileName,
                                Name = varName,
                                Type = varType,
                                Dimensions = dimensions,
                                Size = size,
                                Value = t[position + index + 1].Value
                            });
                        }
                    }

                    position++;
                }
            }
            catch (Exception)
            {
                // ignore for now
            }

            return variables;
        }
    }

}