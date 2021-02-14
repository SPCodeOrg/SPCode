using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser
{
    public partial class Condenser
    {
        private int ConsumeSMTypedef()
        {
            var startIndex = t[position].Index;
            if ((position + 2) < length)
            {
                ++position;
                if (t[position].Kind == TokenKind.Identifier)
                {
                    var name = t[position].Value;
                    for (var iteratePosition = position + 1; iteratePosition < length; ++iteratePosition)
                    {
                        if (t[iteratePosition].Kind == TokenKind.Semicolon)
                        {
                            def.Typedefs.Add(new SMTypedef()
                            {
                                Index = startIndex,
                                Length = t[iteratePosition].Index - startIndex + 1,
                                File = FileName,
                                Name = name,
                                FullName = source.Substring(startIndex, t[iteratePosition].Index - startIndex + 1)
                            });
                            return iteratePosition;
                        }
                    }
                }
            }
            return -1;
        }

        private int ConsumeSMTypeset()
        {
            var startIndex = t[position].Index;
            if ((position + 2) < length)
            {
                ++position;
                if (t[position].Kind == TokenKind.Identifier)
                {
                    var name = t[position].Value;
                    var bracketIndex = 0;
                    for (var iteratePosition = position + 1; iteratePosition < length; ++iteratePosition)
                    {
                        if (t[iteratePosition].Kind == TokenKind.BraceClose)
                        {
                            --bracketIndex;
                            if (bracketIndex == 0)
                            {
                                def.Typedefs.Add(new SMTypedef()
                                {
                                    Index = startIndex,
                                    Length = t[iteratePosition].Index - startIndex + 1,
                                    File = FileName,
                                    Name = name,
                                    FullName = source.Substring(startIndex, t[iteratePosition].Index - startIndex + 1)
                                });
                                return iteratePosition;
                            }
                        }
                        else if (t[iteratePosition].Kind == TokenKind.BraceOpen)
                        {
                            ++bracketIndex;
                        }
                    }
                }
            }
            return -1;
        }
    }
}
