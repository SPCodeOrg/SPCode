using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SPCode.Utils
{
    public static class RegexKeywordsHelper
    {
        public static Regex GetRegexFromKeywords(string[] keywords, bool ForceAtomicRegex = false)
        {
            if (ForceAtomicRegex)
            {
                keywords = ConvertToAtomicRegexAbleStringArray(keywords);
            }

            if (keywords.Length == 0)
            {
                return new Regex("SPEdit_Error"); //hehe 
            }

            var UseAtomicRegex = true;
            for (var j = 0; j < keywords.Length; ++j)
            {
                if (!char.IsLetterOrDigit(keywords[j][0]) ||
                    !char.IsLetterOrDigit(keywords[j][keywords[j].Length - 1]))
                {
                    UseAtomicRegex = false;
                    break;
                }
            }

            var regexBuilder = new StringBuilder();
            if (UseAtomicRegex)
            {
                regexBuilder.Append(@"\b(?>");
            }
            else
            {
                regexBuilder.Append(@"(");
            }

            var orderedKeyWords = new List<string>(keywords);
            var i = 0;
            foreach (var keyword in orderedKeyWords.OrderByDescending(w => w.Length))
            {
                if (i++ > 0)
                {
                    regexBuilder.Append('|');
                }

                if (UseAtomicRegex)
                {
                    regexBuilder.Append(Regex.Escape(keyword));
                }
                else
                {
                    if (char.IsLetterOrDigit(keyword[0]))
                    {
                        regexBuilder.Append(@"\b");
                    }

                    regexBuilder.Append(Regex.Escape(keyword));
                    if (char.IsLetterOrDigit(keyword[keyword.Length - 1]))
                    {
                        regexBuilder.Append(@"\b");
                    }
                }
            }

            if (UseAtomicRegex)
            {
                regexBuilder.Append(@")\b");
            }
            else
            {
                regexBuilder.Append(@")");
            }

            return new Regex(regexBuilder.ToString(), RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
        }

        public static string[] ConvertToAtomicRegexAbleStringArray(string[] keywords)
        {
            var atomicRegexAbleList = new List<string>();
            for (var j = 0; j < keywords.Length; ++j)
            {
                if (keywords[j].Length > 0)
                {
                    if (char.IsLetterOrDigit(keywords[j][0]) &&
                        char.IsLetterOrDigit(keywords[j][keywords[j].Length - 1]))
                    {
                        atomicRegexAbleList.Add(keywords[j]);
                    }
                }
            }

            return atomicRegexAbleList.ToArray();
        }

        public static Regex GetRegexFromKeywords2(string[] keywords)
        {
            var regexBuilder = new StringBuilder(@"\b(?<=[^\s]+\.)(");
            var i = 0;
            foreach (var keyword in keywords)
            {
                if (i++ > 0)
                {
                    regexBuilder.Append("|");
                }

                regexBuilder.Append(keyword);
            }

            regexBuilder.Append(@")\b");
            return new Regex(regexBuilder.ToString(), RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
        }
    }
}
