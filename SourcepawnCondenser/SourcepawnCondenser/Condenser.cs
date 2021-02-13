using System;
using System.Text;
using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser
{
    public partial class Condenser
    {
        private readonly SMDefinition def;

        private readonly string FileName;
        private readonly int length;
        private readonly string source;
        private readonly Token[] t;
        private int position;

        public Condenser(string sourceCode, string fileName)
        {
            t = Tokenizer.Tokenizer.TokenizeString(sourceCode, true).ToArray();
            position = 0;
            length = t.Length;
            def = new SMDefinition();
            source = sourceCode;
            if (fileName.EndsWith(".inc", StringComparison.InvariantCultureIgnoreCase))
            {
                fileName = fileName.Substring(0, fileName.Length - 4);
            }

            FileName = fileName;
        }

        public SMDefinition Condense()
        {
            Token ct;
            while ((ct = t[position]).Kind != TokenKind.EOF)
            {
                switch (ct.Kind)
                {
                    case TokenKind.FunctionIndicator:
                        {
                            var newIndex = ConsumeSMFunction();
                            if (newIndex != -1)
                            {
                                position = newIndex + 1;
                                continue;
                            }

                            break;
                        }
                    case TokenKind.EnumStruct:
                        {
                            var newIndex = ConsumeSMEnumStruct();
                            if (newIndex != -1)
                            {
                                position = newIndex + 1;
                                continue;
                            }

                            break;
                        }
                    case TokenKind.Enum:
                        {
                            var newIndex = ConsumeSMEnum();
                            if (newIndex != -1)
                            {
                                position = newIndex + 1;
                                continue;
                            }

                            break;
                        }
                    case TokenKind.Struct:
                        {
                            var newIndex = ConsumeSMStruct();
                            if (newIndex != -1)
                            {
                                position = newIndex + 1;
                                continue;
                            }

                            break;
                        }
                    case TokenKind.PrePocessorDirective:
                        {
                            var newIndex = ConsumeSMPPDirective();
                            if (newIndex != -1)
                            {
                                position = newIndex + 1;
                                continue;
                            }

                            break;
                        }
                    case TokenKind.Constant:
                        {
                            var newIndex = ConsumeSMConstant();
                            if (newIndex != -1)
                            {
                                position = newIndex + 1;
                                continue;
                            }

                            break;
                        }
                    case TokenKind.MethodMap:
                        {
                            var newIndex = ConsumeSMMethodmap();
                            if (newIndex != -1)
                            {
                                position = newIndex + 1;
                                continue;
                            }

                            break;
                        }
                    case TokenKind.TypeSet:
                        {
                            var newIndex = ConsumeSMTypeset();
                            if (newIndex != -1)
                            {
                                position = newIndex + 1;
                                continue;
                            }

                            break;
                        }
                    case TokenKind.TypeDef:
                        {
                            var newIndex = ConsumeSMTypedef();
                            if (newIndex != -1)
                            {
                                position = newIndex + 1;
                                continue;
                            }

                            break;
                        }
                    case TokenKind.Identifier:
                        {
                            var newIndex = ConsumeSMVariable();
                            if (newIndex != -1)
                            {
                                position = newIndex + 1;
                                continue;
                            }

                            // If Variable is not found try function
                            newIndex = ConsumeSMFunction();
                            if (newIndex != -1)
                            {
                                position = newIndex + 1;
                                continue;
                            }
                        }
                        break;
                }

                ++position;
            }

            def.Sort();
            return def;
        }

        private int BacktraceTestForToken(int StartPosition, TokenKind TestKind, bool IgnoreEOL, bool IgnoreOtherTokens)
        {
            for (var i = StartPosition; i >= 0; --i)
            {
                if (t[i].Kind == TestKind)
                {
                    return i;
                }

                if (IgnoreOtherTokens)
                {
                    continue;
                }

                if (t[i].Kind == TokenKind.EOL && IgnoreEOL)
                {
                    continue;
                }

                return -1;
            }

            return -1;
        }

        private int FortraceTestForToken(int StartPosition, TokenKind TestKind, bool IgnoreEOL, bool IgnoreOtherTokens)
        {
            for (var i = StartPosition; i < length; ++i)
            {
                if (t[i].Kind == TestKind)
                {
                    return i;
                }

                if (IgnoreOtherTokens)
                {
                    continue;
                }

                if (t[i].Kind == TokenKind.EOL && IgnoreEOL)
                {
                    continue;
                }

                return -1;
            }

            return -1;
        }

        public static string TrimComments(string comment)
        {
            var outString = new StringBuilder();
            var lines = comment.Split('\r', '\n');
            string line;
            for (var i = 0; i < lines.Length; ++i)
            {
                line = lines[i].Trim().TrimStart('/', '*', ' ', '\t');
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

        public static string TrimFullname(string name)
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
    }
}