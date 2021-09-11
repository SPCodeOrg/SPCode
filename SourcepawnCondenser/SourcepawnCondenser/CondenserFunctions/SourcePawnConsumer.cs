using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser
{
    public abstract class SourcePawnConsumer
    {
        protected readonly ImmutableList<Token> Tokens;
        protected readonly string FileName;

        protected SourcePawnConsumer(ImmutableList<Token> tokens, string fileName)
        {
            Tokens = tokens;
            FileName = fileName;
        }

        public abstract (SMBaseDefinition? def, int count) Consume(int position);

        protected int BacktraceTestForToken(int position, TokenKind tokenKind, bool ignoreEOL, bool ignoreOther)
        {
            for (var i = position; i >= 0; --i)
            {
                if (Tokens[i].Kind == tokenKind)
                {
                    return i;
                }

                if (ignoreOther)
                {
                    continue;
                }

                if (Tokens[i].Kind == TokenKind.EOL && ignoreEOL)
                {
                    continue;
                }

                return -1;
            }

            return -1;
        }

        protected int FortraceTestForToken(int position, TokenKind tokenKind, bool ignoreEOL, bool ignoreOther)
        {
            for (var i = position; i < Tokens.Count; ++i)
            {
                if (Tokens[i].Kind == tokenKind)
                {
                    return i;
                }

                if (ignoreEOL)
                {
                    continue;
                }

                if (Tokens[i].Kind == TokenKind.EOL && ignoreOther)
                {
                    continue;
                }

                return -1;
            }

            return -1;
        }


        protected string FindComment(int position)
        {
            // Try to match multiline comment
            var commentTokenIndex = BacktraceTestForToken(position - 1, TokenKind.MultiLineComment, true, false);
            if (commentTokenIndex != -1)
            {
                return Tokens[commentTokenIndex].Value;
            }

            // Try to match multiple single line comments
            commentTokenIndex = BacktraceTestForToken(position - 1, TokenKind.SingleLineComment, true, false);
            if (commentTokenIndex == -1)
            {
                // Token not found, return no comments.
                return "";
            }

            var strBuilder = new StringBuilder(Tokens[commentTokenIndex].Value);
            while ((commentTokenIndex =
                BacktraceTestForToken(commentTokenIndex - 1, TokenKind.SingleLineComment, true,
                    false)) != -1)
            {
                strBuilder.Insert(0, Environment.NewLine);
                strBuilder.Insert(0, Tokens[commentTokenIndex].Value);
            }

            return strBuilder.ToString();

        }

        // Returns true if the next to tokens represent an array declaration ( "[]" )
        protected bool IsArray(int position)
        {
            if (Tokens.Count < position + 1)
            {
                return false;
            }
            return Tokens[position].Kind == TokenKind.BracketOpen && Tokens[position+ 1].Kind == TokenKind.BracketClose;
        }
        static public readonly (SMBaseDefinition? def, int count) InvalidValue = (null, 1);
    }
}
//TODO: Move this to another place
namespace EnumExtension
{
    public static class EnumExtensionSplit
    {
        public static IEnumerable<IEnumerable<TSource>> Split<TSource>(
            this IEnumerable<TSource> source,
            Func<TSource, bool> partitionBy,
            bool removeEmptyEntries = false,
            int count = -1)
        {
            int yielded = 0;
            var items = new List<TSource>();
            foreach (var item in source)
            {
                if (!partitionBy(item))
                    items.Add(item);
                else if (!removeEmptyEntries || items.Count > 0)
                {
                    yield return items.ToArray();
                    items.Clear();

                    if (count > 0 && ++yielded == count) yield break;
                }
            }

            if (items.Count > 0) yield return items.ToArray();
        }
    }
}