using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SourcepawnCondenser.Tokenizer;

public static class Tokenizer
{
    public static List<Token> TokenizeString(string source, bool ignoreMultipleEOL)
    {
        var sArray = source.ToCharArray();
        var sArrayLength = sArray.Length;
        var token = new List<Token>((int)Math.Ceiling(sArrayLength * 0.20f));

        //the reservation of the capacity is an empirical measured optimization. The average token to text length is 0.19 (with multiple EOL)
        //To hopefully never extend the inner array, we use 2.3  |  performance gain: around 20%
        for (var i = 0; i < sArrayLength; ++i)
        {
            var c = sArray[i];

            switch (c)
            {
                case ' ':
                case '\t':
                    continue;

                case '\n':
                case '\r':
                {
                    token.Add(new Token("\r\n", TokenKind.EOL, i));
                    if (ignoreMultipleEOL)
                    {
                        while (i + 1 < sArrayLength)
                        {
                            if (sArray[i + 1] == '\n' || sArray[i + 1] == '\r')
                            {
                                ++i;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    else if (c == '\r')
                    {
                        if (i + 1 < sArrayLength)
                        {
                            if (sArray[i + 1] == '\n')
                            {
                                ++i;
                            }
                        }
                    }

                    continue;
                }
                case '{':
                    token.Add(new Token("{", TokenKind.BraceOpen, i));
                    continue;
                case '}':
                    token.Add(new Token("}", TokenKind.BraceClose, i));
                    continue;
                case '(':
                    token.Add(new Token("(", TokenKind.ParenthesisOpen, i));
                    continue;
                case ')':
                    token.Add(new Token(")", TokenKind.ParenthesisClose, i));
                    continue;
                case ';':
                    token.Add(new Token(";", TokenKind.Semicolon, i));
                    continue;
                case ',':
                    token.Add(new Token(",", TokenKind.Comma, i));
                    continue;
                case '=':
                    token.Add(new Token("=", TokenKind.Assignment, i));
                    continue;
                case '/':
                {
                    if (i + 1 < sArrayLength)
                    {
                        switch (sArray[i + 1])
                        {
                            // Single line comment
                            case '/':
                            {
                                var startIndex = i;
                                var endIndex = -1;
                                for (var j = i + 1; j < sArrayLength; ++j)
                                {
                                    if (sArray[j] != '\r' && sArray[j] != '\n')
                                    {
                                        continue;
                                    }

                                    endIndex = j;
                                    break;
                                }

                                if (endIndex == -1)
                                {
                                    token.Add(new Token(source.Substring(startIndex), TokenKind.SingleLineComment,
                                        startIndex));
                                    i = sArrayLength;
                                }
                                else
                                {
                                    token.Add(new Token(source.Substring(startIndex, endIndex - startIndex),
                                        TokenKind.SingleLineComment, startIndex));
                                    i = endIndex - 1;
                                }

                                continue;
                            }
                            //multiline comment
                            case '*' when i + 3 < sArrayLength:
                            {
                                var startIndex = i;
                                var endIndex = -1;
                                for (var j = i + 3; j < sArrayLength; ++j)
                                {
                                    if (sArray[j] == '/')
                                    {
                                        if (sArray[j - 1] == '*')
                                        {
                                            endIndex = j;
                                            break;
                                        }
                                    }
                                }

                                if (endIndex == -1)
                                {
                                    i = sArrayLength;
                                    token.Add(new Token(source.Substring(startIndex), TokenKind.MultiLineComment,
                                        startIndex));
                                }
                                else
                                {
                                    i = endIndex;
                                    token.Add(new Token(source.Substring(startIndex, endIndex - startIndex + 1),
                                        TokenKind.MultiLineComment, startIndex));
                                }

                                continue;
                            }
                        }
                    }

                    break;
                }
                case '"' when i + 1 < sArrayLength:
                {
                    var startIndex = i;
                    var endIndex = -1;
                    for (var j = i + 1; j < sArrayLength; ++j)
                    {
                        if (sArray[j] != '"' || sArray[j - 1] == '\\')
                        {
                            continue;
                        }

                        endIndex = j;
                        break;
                    }

                    if (endIndex != -1)
                    {
                        token.Add(new Token(source.Substring(startIndex, endIndex - startIndex + 1),
                            TokenKind.Quote,
                            startIndex));
                        i = endIndex;
                        continue;
                    }

                    break;
                }
                // TODO: Create a real char token
                case '\'' when i + 1 < sArrayLength:
                {
                    var startIndex = i;
                    var endIndex = -1;
                    for (var j = i + 1; j < sArrayLength; ++j)
                    {
                        if (sArray[j] != '\'' || sArray[j - 1] == '\\')
                        {
                            continue;
                        }

                        endIndex = j;
                        break;
                    }

                    if (endIndex != -1)
                    {
                        token.Add(new Token(source.Substring(startIndex, endIndex - startIndex + 1),
                            TokenKind.Quote,
                            startIndex));
                        i = endIndex;
                        continue;
                    }

                    break;
                }

                //identifier
                case >= 'a' and <= 'z':
                case >= 'A' and <= 'Z':
                case '_':
                {
                    int startIndex = i, endIndex = i + 1;
                    var nextChar = '\0';
                    if (i + 1 < sArrayLength)
                    {
                        nextChar = sArray[i + 1];
                        endIndex = -1;
                        for (var j = i + 1; j < sArrayLength; ++j)
                        {
                            if (IsIdentifierChar(sArray[j]))
                            {
                                continue;
                            }

                            endIndex = j;
                            break;
                        }

                        if (endIndex == -1)
                        {
                            endIndex = sArrayLength;
                        }
                    }

                    if (c != '_' || (c == '_' && IsIdentifierChar(nextChar)))
                    {
                        var identString = source.Substring(startIndex, endIndex - startIndex);
                        switch (identString)
                        {
                            case "native":
                            case "stock":
                            case "forward":
                            case "public":
                            case "normal":
                            case "static":
                            {
                                token.Add(new Token(identString, TokenKind.FunctionIndicator, startIndex));
                                break;
                            }
                            case "enum":
                            {
                                var fullString = source.Substring(startIndex, endIndex - startIndex + 7);
                                token.Add(fullString == "enum struct"
                                    ? new Token(fullString, TokenKind.EnumStruct, startIndex)
                                    : new Token(identString, TokenKind.Enum, startIndex));

                                break;
                            }
                            case "struct":
                            {
                                var enumStructStart = startIndex - 5;
                                if (enumStructStart >= 0)
                                {
                                    // Avoid double matches
                                    var fullString = source.Substring(enumStructStart, endIndex - enumStructStart);
                                    if (fullString == "enum struct")
                                    {
                                        break;
                                    }
                                }

                                token.Add(new Token(identString, TokenKind.Struct, startIndex));
                                break;
                            }
                            case "const":
                            {
                                token.Add(new Token(identString, TokenKind.Constant, startIndex));
                                break;
                            }
                            case "methodmap":
                            {
                                token.Add(new Token(identString, TokenKind.MethodMap, startIndex));
                                break;
                            }
                            case "property":
                            {
                                token.Add(new Token(identString, TokenKind.Property, startIndex));
                                break;
                            }
                            case "typeset":
                            case "funcenum":
                            {
                                token.Add(new Token(identString, TokenKind.TypeSet, startIndex));
                                break;
                            }
                            case "typedef":
                            case "functag":
                            {
                                token.Add(new Token(identString, TokenKind.TypeDef, startIndex));
                                break;
                            }
                            case "new":
                            {
                                token.Add(new Token(identString, TokenKind.New, startIndex));
                                break;
                            }
                            default:
                            {
                                token.Add(new Token(identString, TokenKind.Identifier, startIndex));
                                break;
                            }
                        }

                        i = endIndex - 1;
                        continue;
                    }

                    break;
                }

                //numbers
                case >= '0' and <= '9':
                {
                    var startIndex = i;
                    var endIndex = -1;
                    var gotDecimal = false;
                    var gotExponent = false;
                    for (var j = i + 1; j < sArrayLength; ++j)
                    {
                        if (sArray[j] == '.')
                        {
                            if (!gotDecimal)
                            {
                                if (j + 1 < sArrayLength)
                                {
                                    if (sArray[j + 1] >= '0' && sArray[j + 1] <= '9')
                                    {
                                        gotDecimal = true;
                                        continue;
                                    }
                                }
                            }

                            endIndex = j - 1;
                            break;
                        }

                        if (sArray[j] == 'e' || sArray[j] == 'E')
                        {
                            if (!gotExponent)
                            {
                                if (j + 1 < sArrayLength)
                                {
                                    if (sArray[j + 1] == '+' || sArray[j + 1] == '-')
                                    {
                                        if (j + 2 < sArrayLength)
                                        {
                                            if (sArray[j + 2] >= '0' && sArray[j + 2] <= '9')
                                            {
                                                ++j;
                                                gotDecimal = gotExponent = true;
                                                continue;
                                            }
                                        }
                                    }
                                    else if (sArray[j + 1] >= '0' && sArray[j + 1] <= '9')
                                    {
                                        gotDecimal = gotExponent = true;
                                        continue;
                                    }
                                }
                            }

                            endIndex = j - 1;
                            break;
                        }

                        if (sArray[j] >= '0' && sArray[j] <= '9')
                        {
                            continue;
                        }

                        endIndex = j - 1;
                        break;
                    }

                    if (endIndex == -1)
                    {
                        endIndex = sArrayLength - 1;
                    }

                    token.Add(new Token(source.Substring(startIndex, endIndex - startIndex + 1), TokenKind.Number,
                        startIndex));
                    i = endIndex;
                    continue;
                }

                // Preprocessor directives
                case '#':
                {
                    var startIndex = i;
                    if (i + 1 < sArrayLength)
                    {
                        if (char.IsLetter(sArray[i + 1]))
                        {
                            var endIndex = i + 1;
                            for (var j = i + 1; j < sArrayLength; ++j)
                            {
                                if (char.IsLetter(sArray[j]))
                                {
                                    continue;
                                }

                                endIndex = j;
                                break;
                            }

                            var directiveString = source.Substring(startIndex, endIndex - startIndex);
                            token.Add(new Token(directiveString, TokenKind.PreprocessorDirective, startIndex));

                            if (directiveString == "#define" &&
                                (sArray[endIndex] == ' ' || sArray[endIndex] == '\t'))
                            {
                                while (sArray[endIndex - 1] == ' ')
                                {
                                    endIndex++;
                                }

                                var name = new StringBuilder();
                                var found = false;

                                for (var j = endIndex + 1; j < sArrayLength; ++j)
                                {
                                    if (sArray[j] == '\n' || sArray[j] == '\r')
                                    {
                                        i = j - 1;
                                        break;
                                    }

                                    if (sArray[j] == ' ' || sArray[j] == '\t')
                                    {
                                        if (found)
                                        {
                                            i = j - 1;
                                            break;
                                        }
                                        continue;
                                    }

                                    found = true;
                                    name.Append(sArray[j]);
                                }

                                if (name.ToString().Trim().Length != 0)
                                {
                                    token.Add(
                                        new Token(name.ToString(), TokenKind.Identifier, endIndex + 1));
                                }
                            }

                            for (var j = i + 1; j < sArrayLength; ++j)
                            {
                                if (sArray[j] != '\n' && sArray[j] != '\r')
                                {
                                    continue;
                                }

                                i = j - 1;
                                break;
                            }

                            continue;
                        }
                    }

                    break;
                }
            }

            token.Add(new Token(c, TokenKind.Character, i));
        }

        token.Add(new Token("", TokenKind.EOF, sArrayLength));
        
        
        return token;
    }


    private static bool IsIdentifierChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }
}