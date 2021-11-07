using System.Collections.Generic;
using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser
{
    public partial class Condenser
    {
        private int ConsumeSMVariable()
        {
            if (position + 3 < length)
            {
                var startIndex = t[position].Index;

                var varType = t[position].Value;
                string varName;
                var size = new List<string>();
                var dimensions = 0;
                var index = -1;

                // Dynamic array: "char[] x = new char[64];"

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

                        return -1;
                    }

                    varName = t[position + index].Value;

                    if (t[position + index + 1].Kind != TokenKind.Assignment ||
                        t[position + index + 2].Kind != TokenKind.New || t[position + index + 3].Value != varType)
                    {
                        return -1;
                    }

                    for (var i = index + 4; i < length - 2; i += 3)
                    {
                        if (t[position + i].Kind == TokenKind.Semicolon)
                        {
                            def.Variables.Add(new SMVariable
                            {
                                Index = startIndex,
                                Length = t[position + i].Index - startIndex,
                                File = FileName,
                                Name = varName,
                                Type = varType,
                                Dimensions = dimensions,
                                Size = size,
                            });
                            return position + i;
                        }

                        if (t[position + i].Value == "[" &&
                            (t[position + i + 1].Kind == TokenKind.Number ||
                             t[position + i + 1].Kind == TokenKind.Identifier) &&
                            t[position + i + 2].Value == "]")
                        {
                            size.Add(t[position + i + 1].Value);
                            continue;
                        }

                        return -1;
                    }

                    return -1;
                }

                if (t[position + 1].Kind != TokenKind.Identifier)
                {
                    return -1;
                }

                varName = t[position + 1].Value;

                switch (t[position + 2].Kind)
                {
                    // Simple var match: "int x;"
                    case TokenKind.Semicolon:
                        def.Variables.Add(new SMVariable
                        {
                            Index = startIndex,
                            Length = t[position + 2].Index - startIndex,
                            File = FileName,
                            Name = varName,
                            Type = varType
                        });
                        return position + 2;
                    // Assign var match: "int x = 5"
                    case TokenKind.Assignment when t[position + 4].Kind == TokenKind.Semicolon &&
                                                   (t[position + 3].Kind == TokenKind.Number ||
                                                    t[position + 3].Kind == TokenKind.Quote ||
                                                    t[position + 3].Kind == TokenKind.Identifier):
                        def.Variables.Add(new SMVariable
                        {
                            Index = startIndex,
                            Length = t[position + 4].Index - startIndex,
                            File = FileName,
                            Name = varName,
                            Type = varType,
                            Value = t[position + 3].Value
                        });
                        return position + 4;
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
                            return -1;
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

                if (t[position + index].Kind == TokenKind.Semicolon && !requireAssign)
                {
                    def.Variables.Add(new SMVariable
                    {
                        Index = startIndex,
                        Length = t[position + index].Index - startIndex,
                        File = FileName,
                        Name = varName,
                        Type = varType,
                        Dimensions = dimensions,
                        Size = size
                    });
                    return position + index;
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

                        def.Variables.Add(new SMVariable
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
                            return -1;
                        }

                        def.Variables.Add(new SMVariable
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
            }

            return -1;
        }
    }
}