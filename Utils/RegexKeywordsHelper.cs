using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SPCode.Utils;

public static class RegexKeywordsHelper
{
    /// <summary>
    /// Converts a string list a regexp matching all the given word.
    /// </summary>
    /// <param name="keywords">The words list</param>
    /// <returns></returns>
    public static Regex GetRegexFromKeywords(string[] keywords)
    {
        if (keywords.Length == 0)
        {
            return new Regex("SPEdit_Error"); //We must not return regex that matches any string
        }
        
        return new Regex(
            @$"\b({string.Join("|", keywords)})\b",
            RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
    }

    public static Regex GetRegexFromKeywords(List<string> keywords)
    {
        if (keywords.Count == 0)
        {
            return new Regex("SPEdit_Error"); //We must not return regex that matches any string
        }

        return new Regex(
            @$"\b({string.Join("|", keywords.Select(Regex.Escape))})\b",
            RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
    }

    
    /// <summary>
    /// Used the match function class like "PrintToChat(...)"
    /// </summary>
    public static Regex GetFunctionRegex(string[] keywords)
    {
        if (keywords.Length == 0)
        {
            return new Regex("SPEdit_Error"); //We must not return regex that matches any string
        }
        
        return new Regex(
            @$"\b(?<!\.)({string.Join("|", keywords.Select(Regex.Escape))})\b",
            RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
    }

    /// <summary>
    /// Used the match method class like "myArr.Push"
    /// </summary>
    public static Regex GetMethodRegex(List<string> keywords)
    {
        if (keywords.Count == 0)
        {
            return new Regex("SPEdit_Error"); //We must not return regex that matches any string
        }
        
        return new Regex(
            @$"\b(?<=[^\s]+\.)({string.Join("|", keywords.Select(Regex.Escape))})\b",
            RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
    }
}