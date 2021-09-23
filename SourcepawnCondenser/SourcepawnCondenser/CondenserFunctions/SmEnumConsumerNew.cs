using System.Collections.Immutable;
using System.Linq;
using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser
{
    public class SmEnumConsumer : SourcePawnConsumer
    {
        public SmEnumConsumer(ImmutableList<Token> tokens, string fileName) : base(tokens, fileName)
        {
        }

        public override (SMBaseDefinition? def, int count) Consume(int position)
        {
            var tokens = Tokens.GetRange(position, Tokens.Count - position);
            if (tokens.Count < 3)
            {
                return InvalidValue;
            }

            // TODO: Check if this is enough or we must check the second token to be != from "struct"
            if (tokens.First().Kind != TokenKind.Enum)
            {
                return InvalidValue;
            }

            var enumName = "";
            var index = 1;
            if (tokens[1].Kind == TokenKind.Identifier)
            {
                enumName = tokens[1].Value;
                index++;
            }

            if (tokens[index] != TokenKind.BraceOpen)
            {
                return (new SMEnum(tokens, index, FileName, enumName, ));
            }
            var startIndex = _tokens[_position].Index;
            if ((_position + 1) < _length)
            {
                var iteratePosition = _position;
                while ((iteratePosition + 1) < _length && _tokens[iteratePosition].Kind != TokenKind.BraceOpen)
                {
                    if (_tokens[iteratePosition].Kind == TokenKind.Identifier)
                    {
                        enumName = _tokens[iteratePosition].Value;
                    }
                    ++iteratePosition;
                }
                var braceState = 0;
                var inIgnoreMode = false;
                var endTokenIndex = -1;
                var entries = new List<string>();
                for (; iteratePosition < _length; ++iteratePosition)
                {
                    if (_tokens[iteratePosition].Kind == TokenKind.BraceOpen)
                    {
                        ++braceState;
                        continue;
                    }
                    if (_tokens[iteratePosition].Kind == TokenKind.BraceClose)
                    {
                        --braceState;
                        if (braceState == 0)
                        {
                            endTokenIndex = iteratePosition;
                            break;
                        }
                        continue;
                    }
                    if (inIgnoreMode)
                    {
                        if (_tokens[iteratePosition].Kind == TokenKind.Comma)
                        {
                            inIgnoreMode = false;
                        }
                        continue;
                    }
                    if (_tokens[iteratePosition].Kind == TokenKind.Identifier)
                    {
                        entries.Add(_tokens[iteratePosition].Value);
                        inIgnoreMode = true;
                    }
                }
                if (endTokenIndex == -1)
                {
                    return -1;
                }
                _def.Enums.Add(new SMEnum()
                {
                    Index = startIndex,
                    Length = _tokens[endTokenIndex].Index - startIndex + 1,
                    File = _fileName,
                    Entries = entries.ToArray(),
                    Name = enumName
                });
                return endTokenIndex;
            }
            return -1;
            


            if (tokens.First().Value != "#define")
            {
                return InvalidValue;
            }

            if (tokens[1].Kind != TokenKind.Identifier)
            {
                return InvalidValue;
            }

            var def = new SMDefine(tokens, 1,
                FileName, tokens[1].Value, "");

            var eof = tokens.FindIndex(token => token.Kind == TokenKind.EOF);
            return eof != -1 ? (def, eof) : (def, 1);
            
        }
    }
}