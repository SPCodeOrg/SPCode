using System;
using System.Collections.Generic;
using System.Text;
using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser;

public partial class Condenser
{
    private readonly SMDefinition _def;

    private readonly string _fileName;
    private readonly int _length;
    private readonly string _source;

    private readonly Token[] _tokens;
    private int _position;

    public Condenser(string sourceCode, string fileName)
    {
        _tokens = Tokenizer.Tokenizer.TokenizeString(sourceCode, true).ToArray();
        _position = 0;
        _length = _tokens.Length;
        _def = new SMDefinition();
        _source = sourceCode;
        if (fileName.EndsWith(".inc", StringComparison.InvariantCultureIgnoreCase))
        {
            fileName = fileName.Substring(0, fileName.Length - 4);
        }

        _fileName = fileName;
    }

    public SMDefinition Condense()
    {
        Token ct;
        while ((ct = _tokens[_position]).Kind != TokenKind.EOF)
        {
            var index = ct.Kind switch
            {
                TokenKind.FunctionIndicator => ConsumeSMFunction(),
                TokenKind.EnumStruct => ConsumeSMEnumStruct(),
                TokenKind.Enum => ConsumeSMEnum(),
                TokenKind.Struct => ConsumeSMStruct(),
                TokenKind.PreprocessorDirective => ConsumeSMPPDirective(),
                TokenKind.Constant => ConsumeSMConstant(),
                TokenKind.MethodMap => ConsumeSMMethodmap(),
                TokenKind.TypeSet => ConsumeSMTypeset(),
                TokenKind.TypeDef => ConsumeSMTypedef(),
                TokenKind.Identifier => ConsumeSMIdentifier(),
                _ => -1,
            };
            if (index != -1)
            {
                _position = index + 1;
                continue;
            }

            ++_position;
        }

        _def.Sort();
        return _def;
    }

    private int BacktraceTestForToken(int startPosition, TokenKind testKind, bool ignoreEol, bool ignoreOtherTokens)
    {
        for (var i = startPosition; i >= 0; --i)
        {
            if (ignoreOtherTokens || (_tokens[i].Kind == TokenKind.EOL && ignoreEol))
            {
                continue;
            }

            if (_tokens[i].Kind == testKind)
            {
                return i;
            }
        }

        return -1;
    }

    private int FortraceTestForToken(int startPosition, TokenKind testKind, bool ignoreEol, bool ignoreOtherTokens)
    {
        for (var i = startPosition; i < _length; ++i)
        {
            if (ignoreOtherTokens || (_tokens[i].Kind == TokenKind.EOL && ignoreEol))
            {
                continue;
            }

            if (_tokens[i].Kind == testKind)
            {
                return i;
            }
        }

        return -1;
    }

    private static string TrimComments(string comment)
    {
        var outString = new StringBuilder();
        var lines = comment.Split('\r', '\n');

        for (var i = 0; i < lines.Length; ++i)
        {
            var line = lines[i].Trim().TrimStart('/', '*', ' ', '\t');
            if (!string.IsNullOrWhiteSpace(line))
            {
                if (i > 0)
                {
                    outString.AppendLine();
                }

                if (line.StartsWith("@param"))
                {
                    outString.Append(FormatParamLineString(line));
                }
                else
                {
                    outString.Append(line);
                }
            }
        }

        return outString.ToString().Trim();
    }

    private static string TrimFullname(string name)
    {
        var outString = new StringBuilder();
        var lines = name.Split('\r', '\n');
        for (var i = 0; i < lines.Length; ++i)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
            {
                if (i > 0)
                {
                    outString.Append(" ");
                }

                outString.Append(lines[i].Trim(' ', '\t'));
            }
        }

        return outString.ToString();
    }

    private static string FormatParamLineString(string line)
    {
        var split = line.Replace('\t', ' ').Split(new[] { ' ' }, 3);
        if (split.Length > 2)
        {
            return ("@param " + split[1]).PadRight(24, ' ') + " " + split[2].Trim(' ', '\t');
        }

        return line;
    }

    private int ConsumeSMIdentifier()
    {
        var index = ConsumeSMVariable();
        return index == -1 ? ConsumeSMFunction() : index;
    }
}