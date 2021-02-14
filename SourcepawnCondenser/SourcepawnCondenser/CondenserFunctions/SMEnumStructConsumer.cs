using System;
using System.Collections.Generic;
using System.Text;
using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser
{
    public partial class Condenser
    {
        private int ConsumeSMEnumStruct()
        {
            var startIndex = t[position].Index;
            var iteratePosition = position + 1;
            if (position + 4 < length)
            {
                var enumStructName = string.Empty;
                var methods = new List<SMEnumStructMethod>();
                var fields = new List<SMEnumStructField>();
                if (t[iteratePosition].Kind == TokenKind.Identifier)
                {
                    enumStructName = t[iteratePosition++].Value;
                }

                var enteredBlock = false;
                var braceIndex = 0;
                var lastIndex = -1;
                for (; iteratePosition < length; ++iteratePosition)
                {
                    if (t[iteratePosition].Kind == TokenKind.BraceOpen)
                    {
                        ++braceIndex;
                        enteredBlock = true;
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
                    else if (enteredBlock)
                    {
                        if (t[iteratePosition].Kind == TokenKind.FunctionIndicator ||
                            (t[iteratePosition].Kind == TokenKind.Identifier &&
                             t[iteratePosition + 1].Kind == TokenKind.Identifier &&
                             t[iteratePosition + 2].Kind == TokenKind.ParenthesisOpen) ||
                            (t[iteratePosition].Kind == TokenKind.Identifier &&
                             t[iteratePosition + 1].Kind == TokenKind.ParenthesisOpen))
                        {
                            var mStartIndex = t[iteratePosition].Index;
                            var functionCommentString = string.Empty;
                            var commentTokenIndex = BacktraceTestForToken(iteratePosition - 1,
                                TokenKind.MultiLineComment, true, false);
                            if (commentTokenIndex == -1)
                            {
                                commentTokenIndex = BacktraceTestForToken(iteratePosition - 1,
                                    TokenKind.SingleLineComment, true, false);
                                if (commentTokenIndex != -1)
                                {
                                    var strBuilder = new StringBuilder(t[commentTokenIndex].Value);
                                    while ((commentTokenIndex = BacktraceTestForToken(commentTokenIndex - 1,
                                        TokenKind.SingleLineComment, true, false)) != -1)
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

                                        ParsingIndicators = false;
                                    }

                                    if (t[i].Kind == TokenKind.Identifier && AwaitingName)
                                    {
                                        if (i + 1 < length)
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
                                            if (i + 1 < length)
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

                                    if (t[i].Kind == TokenKind.Identifier && !InSearchForComma)
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
                                methods.Add(new SMEnumStructMethod
                                {
                                    Index = mStartIndex,
                                    Name = methodName,
                                    ReturnType = methodReturnValue,
                                    MethodKind = functionIndicators.ToArray(),
                                    Parameters = parameters.ToArray(),
                                    FullName = TrimFullname(source.Substring(mStartIndex, mEndIndex - mStartIndex + 1)),
                                    Length = mEndIndex - mStartIndex + 1,
                                    CommentString = TrimComments(functionCommentString),
                                    MethodmapName = enumStructName,
                                    File = FileName
                                });
                            }
                        }
                        else if (t[iteratePosition].Kind == TokenKind.Identifier)
                        {
                            var fStartIndex = t[iteratePosition].Index;
                            var fEndIndex = fStartIndex;
                            if (iteratePosition - 1 >= 0)
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
                                        if (j + 1 < length)
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
                                fields.Add(new SMEnumStructField
                                {
                                    Index = fStartIndex,
                                    Length = fEndIndex - fStartIndex + 1,
                                    Name = fieldName,
                                    File = FileName,
                                    MethodmapName = enumStructName,
                                    FullName = source.Substring(fStartIndex, fEndIndex - fStartIndex + 1)
                                });
                            }
                        }
                    }
                }

                if (enteredBlock && braceIndex == 0)
                {
                    var mm = new SMEnumStruct
                    {
                        Index = startIndex,
                        Length = t[lastIndex].Index - startIndex + 1,
                        Name = enumStructName,
                        File = FileName,
                    };
                    mm.Methods.AddRange(methods);
                    mm.Fields.AddRange(fields);
                    def.EnumStructs.Add(mm);
                    position = lastIndex;
                }
            }

            return -1;
        }

    }
}