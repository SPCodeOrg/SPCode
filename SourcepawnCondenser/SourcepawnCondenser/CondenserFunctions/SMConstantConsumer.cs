using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser
{
    public partial class Condenser
    {
        private int ConsumeSMConstant()
        {
            if (position + 2 < length)
            {
                var startIndex = t[position].Index;
                var foundIdentifier = false;
                var foundAssignment = false;
                var constantName = string.Empty;
                for (var i = position + 2; i < length; ++i)
                {
                    if (t[i].Kind == TokenKind.Semicolon)
                    {
                        if (!foundIdentifier)
                        {
                            if (t[i - 1].Kind == TokenKind.Identifier)
                            {
                                constantName = t[i - 1].Value;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(constantName))
                        {
                            def.Constants.Add(new SMConstant
                            {
                                Index = startIndex,
                                Length = t[i].Index - startIndex,
                                File = FileName,
                                Name = constantName
                            });
                        }

                        return i;
                    }

                    if (t[i].Kind == TokenKind.Assignment)
                    {
                        foundAssignment = true;
                        if (t[i - 1].Kind == TokenKind.Identifier)
                        {
                            foundIdentifier = true;
                            constantName = t[i - 1].Value;
                        }
                    }
                    else if (t[i].Kind == TokenKind.Character && !foundAssignment)
                    {
                        if (t[i].Value == "[")
                        {
                            if (t[i - 1].Kind == TokenKind.Identifier)
                            {
                                foundIdentifier = true;
                                constantName = t[i - 1].Value;
                            }
                        }
                    }
                    else if (t[i].Kind == TokenKind.EOL) //failsafe
                    {
                        return i;
                    }
                }
            }

            return -1;
        }
    }
}