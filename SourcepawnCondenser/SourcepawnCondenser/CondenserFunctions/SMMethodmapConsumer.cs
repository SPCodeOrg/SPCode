using System;
using System.Collections.Generic;
using System.Text;
using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser
{
    public partial class Condenser
    {
        private int ConsumeSMMethodmap()
        {
            var startIndex = t[position].Index;
            var iteratePosition = position + 1;
            if ((position + 4) < length)
            {
                var methodMapName = string.Empty;
                var methodMapType = string.Empty;
                var methods = new List<SMMethodmapMethod>();
                var fields = new List<SMMethodmapField>();
                if (t[iteratePosition].Kind == TokenKind.Identifier)
                {
                    if (t[iteratePosition + 1].Kind == TokenKind.Identifier)
                    {
                        methodMapType = t[iteratePosition].Value;
                        ++iteratePosition;
                        methodMapName = t[iteratePosition].Value;
                    }
                    else
                    {
                        methodMapName = t[iteratePosition].Value;
                    }
                    ++iteratePosition;
                }
                var inheriteType = string.Empty;
                var enteredBlock = false;
                var braceIndex = 0;
                var lastIndex = -1;
                for (; iteratePosition < length; ++iteratePosition)
                {
                    if (t[iteratePosition].Kind == TokenKind.BraceOpen)
                    {
                        ++braceIndex;
                        enteredBlock = true;
                        continue;
                    }
                    else if (t[iteratePosition].Kind == TokenKind.BraceClose)
                    {
                        --braceIndex;
                        if (braceIndex <= 0)
                        {
                            lastIndex = iteratePosition;
                            break;
                        }
                    }
                    else if (braceIndex == 0 && t[iteratePosition].Kind == TokenKind.Character)
                    {
                        if (t[iteratePosition].Value == "<")
                        {
                            if ((iteratePosition + 1) < length)
                            {
                                if (t[iteratePosition + 1].Kind == TokenKind.Identifier)
                                {
                                    inheriteType = t[iteratePosition + 1].Value;
                                    ++iteratePosition;
                                    continue;
                                }
                            }
                        }
                    }
                    else if (enteredBlock)
                    {
                        if (t[iteratePosition].Kind == TokenKind.FunctionIndicator)
                        {
                            var mStartIndex = t[iteratePosition].Index;
                            var functionCommentString = string.Empty;
                            var commentTokenIndex = BacktraceTestForToken(iteratePosition - 1, TokenKind.MultiLineComment, true, false);
                            if (commentTokenIndex == -1)
                            {
                                commentTokenIndex = BacktraceTestForToken(iteratePosition - 1, TokenKind.SingleLineComment, true, false);
                                if (commentTokenIndex != -1)
                                {
                                    var strBuilder = new StringBuilder(t[commentTokenIndex].Value);
                                    while ((commentTokenIndex = BacktraceTestForToken(commentTokenIndex - 1, TokenKind.SingleLineComment, true, false)) != -1)
                                    {
                                        strBuilder.Insert(0, Environment.NewLine);
                                        strBuilder.Insert(0, t[commentTokenIndex].Value);
                                    }
                                    functionCommentString = strBuilder.ToString();
                                }
                            }
                            else
                            {
                                functionCommentString = t[commentTokenIndex].Value;
                            }
                            var mEndIndex = mStartIndex;
                            var functionIndicators = new List<string>();
                            var parameters = new List<string>();
                            var methodName = string.Empty;
                            var methodReturnValue = string.Empty;
                            var ParsingIndicators = true;
                            var InCodeSection = false;
                            var ParenthesisIndex = 0;
                            var mBraceIndex = 0;
                            var AwaitingName = true;
                            var lastFoundParam = string.Empty;
                            var foundCurentParameter = false;
                            var InSearchForComma = false;
                            for (var i = iteratePosition; i < length; ++i)
                            {
                                if (InCodeSection)
                                {
                                    if (t[i].Kind == TokenKind.BraceOpen)
                                    {
                                        ++mBraceIndex;
                                    }
                                    else if (t[i].Kind == TokenKind.BraceClose)
                                    {
                                        --mBraceIndex;
                                        if (mBraceIndex <= 0)
                                        {
                                            iteratePosition = i;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    if (ParsingIndicators)
                                    {
                                        if (t[i].Kind == TokenKind.FunctionIndicator)
                                        {
                                            functionIndicators.Add(t[i].Value);
                                            continue;
                                        }
                                        else
                                        {
                                            ParsingIndicators = false;
                                        }
                                    }
                                    if (t[i].Kind == TokenKind.Identifier && AwaitingName)
                                    {
                                        if ((i + 1) < length)
                                        {
                                            if (t[i + 1].Kind == TokenKind.Identifier)
                                            {
                                                methodReturnValue = t[i].Value;
                                                methodName = t[i + 1].Value;
                                                ++i;
                                            }
                                            else
                                            {
                                                methodName = t[i].Value;
                                            }
                                            AwaitingName = false;
                                        }
                                        continue;
                                    }
                                    if (t[i].Kind == TokenKind.ParenthesisOpen)
                                    {
                                        ++ParenthesisIndex;
                                        continue;
                                    }
                                    if (t[i].Kind == TokenKind.ParenthesisClose)
                                    {
                                        --ParenthesisIndex;
                                        if (ParenthesisIndex == 0)
                                        {
                                            if (foundCurentParameter)
                                            {
                                                parameters.Add(lastFoundParam);
                                                lastFoundParam = string.Empty;
                                            }
                                            InCodeSection = true;
                                            if ((i + 1) < length)
                                            {
                                                if (t[i + 1].Kind == TokenKind.Semicolon)
                                                {
                                                    iteratePosition = i + 1;
                                                    mEndIndex = t[i + 1].Index;
                                                    break;
                                                }
                                                iteratePosition = i;
                                                mEndIndex = t[i].Index;
                                            }
                                        }
                                        continue;
                                    }
                                    if ((t[i].Kind == TokenKind.Identifier) && (!InSearchForComma))
                                    {
                                        lastFoundParam = t[i].Value;
                                        foundCurentParameter = true;
                                        continue;
                                    }
                                    if (t[i].Kind == TokenKind.Comma)
                                    {
                                        parameters.Add(lastFoundParam);
                                        lastFoundParam = string.Empty;
                                        InSearchForComma = false;
                                    }
                                    else if (t[i].Kind == TokenKind.Assignment)
                                    {
                                        InSearchForComma = true;
                                    }
                                }
                            }
                            if (mStartIndex < mEndIndex)
                            {
                                methods.Add(new SMMethodmapMethod()
                                {
                                    Index = mStartIndex,
                                    Name = methodName,
                                    ReturnType = methodReturnValue,
                                    MethodKind = functionIndicators.ToArray(),
                                    Parameters = parameters.ToArray(),
                                    FullName = TrimFullname(source.Substring(mStartIndex, mEndIndex - mStartIndex + 1)),
                                    Length = mEndIndex - mStartIndex + 1,
                                    CommentString = Condenser.TrimComments(functionCommentString),
                                    MethodmapName = methodMapName,
                                    File = FileName
                                });
                            }
                        }
                        else if (t[iteratePosition].Kind == TokenKind.Property)
                        {
                            var fStartIndex = t[iteratePosition].Index;
                            var fEndIndex = fStartIndex;
                            if ((iteratePosition - 1) >= 0)
                            {
                                if (t[iteratePosition - 1].Kind == TokenKind.FunctionIndicator)
                                {
                                    fStartIndex = t[iteratePosition - 1].Index;
                                }
                            }
                            var fieldName = string.Empty;
                            var InPureSemicolonSearch = false;
                            var fBracketIndex = 0;
                            for (var j = iteratePosition; j < length; ++j)
                            {
                                if (t[j].Kind == TokenKind.Identifier && !InPureSemicolonSearch)
                                {
                                    fieldName = t[j].Value;
                                    continue;
                                }
                                if (t[j].Kind == TokenKind.Assignment)
                                {
                                    InPureSemicolonSearch = true;
                                    continue;
                                }
                                if (t[j].Kind == TokenKind.Semicolon)
                                {
                                    if (fStartIndex == fEndIndex && fBracketIndex == 0)
                                    {
                                        iteratePosition = j;
                                        fEndIndex = t[j].Index;
                                        break;
                                    }
                                }
                                if (t[j].Kind == TokenKind.BraceOpen)
                                {
                                    if (!InPureSemicolonSearch)
                                    {
                                        InPureSemicolonSearch = true;
                                        fEndIndex = t[j].Index - 1;
                                    }
                                    ++fBracketIndex;
                                }
                                else if (t[j].Kind == TokenKind.BraceClose)
                                {
                                    --fBracketIndex;
                                    if (fBracketIndex == 0)
                                    {
                                        if ((j + 1) < length)
                                        {
                                            if (t[j + 1].Kind == TokenKind.Semicolon)
                                            {
                                                iteratePosition = j + 1;
                                            }
                                            else
                                            {
                                                iteratePosition = j;
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                            if (fStartIndex < fEndIndex)
                            {
                                fields.Add(new SMMethodmapField()
                                {
                                    Index = fStartIndex,
                                    Length = fEndIndex - fStartIndex + 1,
                                    Name = fieldName,
                                    File = FileName,
                                    MethodmapName = methodMapName,
                                    FullName = source.Substring(fStartIndex, fEndIndex - fStartIndex + 1)
                                });
                            }
                        }
                    }
                }
                if (enteredBlock && braceIndex == 0)
                {
                    var mm = new SMMethodmap()
                    {
                        Index = startIndex,
                        Length = t[lastIndex].Index - startIndex + 1,
                        Name = methodMapName,
                        File = FileName,
                        Type = methodMapType,
                        InheritedType = inheriteType
                    };
                    mm.Methods.AddRange(methods);
                    mm.Fields.AddRange(fields);
                    def.Methodmaps.Add(mm);
                    position = lastIndex;
                }
            }
            return -1;
        }
    }
}
