using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using SourcepawnCondenser.SourcemodDefinition;
using SPCode.Utils;

namespace SPCode.UI.Components;

public class AeonEditorHighlighting : IHighlightingDefinition
{
    public string Name => "SM";
    private readonly SMDefinition smDef;

    public AeonEditorHighlighting() { }
    public AeonEditorHighlighting(SMDefinition smDef)
    {
        this.smDef = smDef;
    }

    public HighlightingRuleSet MainRuleSet
    {
        get
        {
            // Brushes
            var commentBrush = new SimpleHighlightingBrush(Program.OptionsObject.SH_Comments);
            var stringBrush = new SimpleHighlightingBrush(Program.OptionsObject.SH_Strings);
            var deprecatedBrush = new SimpleHighlightingBrush(Program.OptionsObject.SH_Deprecated);
            var preprocessorBrush = new SimpleHighlightingBrush(Program.OptionsObject.SH_PreProcessor);
            var typeValuesBrush = new SimpleHighlightingBrush(Program.OptionsObject.SH_TypesValues);
            var mainKeywordsBrush = new SimpleHighlightingBrush(Program.OptionsObject.SH_Keywords);
            var contextKeywordsBrush = new SimpleHighlightingBrush(Program.OptionsObject.SH_ContextKeywords);
            var typesBrush = new SimpleHighlightingBrush(Program.OptionsObject.SH_Types);
            var charBrush = new SimpleHighlightingBrush(Program.OptionsObject.SH_Chars);
            var numberBrush = new SimpleHighlightingBrush(Program.OptionsObject.SH_Numbers);
            var specialCharBrush = new SimpleHighlightingBrush(Program.OptionsObject.SH_SpecialCharacters);
            var constantBrush = new SimpleHighlightingBrush(Program.OptionsObject.SH_Constants);
            var functionBrush = new SimpleHighlightingBrush(Program.OptionsObject.SH_Functions);
            var methodBrush = new SimpleHighlightingBrush(Program.OptionsObject.SH_Methods);
            var unknownFunctionBrush = new SimpleHighlightingBrush(Program.OptionsObject.SH_UnkownFunctions);
            var commentsMarkerBrush = new SimpleHighlightingBrush(Program.OptionsObject.SH_CommentsMarker);

            // RULESET 1: Comment Markers
            var commentMarkerSet = new HighlightingRuleSet
            {
                Name = "CommentMarkerSet"
            };
            commentMarkerSet.Rules.Add(new HighlightingRule
            {
                Regex = RegexKeywordsHelper.GetRegexFromKeywords(new[]
                    {"TODO", "FIX", "FIXME", "HACK", "WORKAROUND", "BUG"}),
                Color = new HighlightingColor
                {
                    Foreground = commentsMarkerBrush,
                    FontWeight = FontWeights.Bold,
                }
            });


            // RULESET 2: Exclude inner single line comment (backslash to escape inside strings)
            var excludeInnerSingleLineComment = new HighlightingRuleSet();

            excludeInnerSingleLineComment.Spans.Add(new HighlightingSpan
            {
                StartExpression = new Regex(@"\\"),
                EndExpression = new Regex(@".")
            });


            // RULESET 3: Main ruleset
            var rs = new HighlightingRuleSet();

            // Create spans

            rs.Spans.Add(new HighlightingSpan // singleline comments
            {
                StartExpression = new Regex(@"//", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture),
                EndExpression = new Regex(@"$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture),
                SpanColor = new HighlightingColor { Foreground = commentBrush },
                StartColor = new HighlightingColor { Foreground = commentBrush },
                EndColor = new HighlightingColor { Foreground = commentBrush },
                RuleSet = commentMarkerSet
            });
            rs.Spans.Add(new HighlightingSpan // multiline comments
            {
                StartExpression = new Regex(@"/\*",
                    RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline),
                EndExpression = new Regex(@"\*/",
                    RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline),
                SpanColor = new HighlightingColor { Foreground = commentBrush },
                StartColor = new HighlightingColor { Foreground = commentBrush },
                EndColor = new HighlightingColor { Foreground = commentBrush },
                RuleSet = commentMarkerSet
            });
            rs.Spans.Add(new HighlightingSpan // strings
            {
                StartExpression = new Regex(@"(?<!')""",
                    RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture),
                EndExpression = new Regex(@"""", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture),
                SpanColor = new HighlightingColor { Foreground = stringBrush },
                StartColor = new HighlightingColor { Foreground = stringBrush },
                EndColor = new HighlightingColor { Foreground = stringBrush },
                RuleSet = excludeInnerSingleLineComment
            });

            // Apply deprecated syntax rules

            if (Program.OptionsObject.SH_HighlightDeprecateds)
            {
                rs.Rules.Add(new HighlightingRule // deprecated variable declaration
                {
                    Regex = new Regex(@"^\s*(decl|new)\s+([a-zA-z_][a-zA-z1-9_]*:)?",
                        RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.ExplicitCapture),
                    Color = new HighlightingColor { Foreground = deprecatedBrush }
                });
                rs.Rules.Add(new HighlightingRule // deprecated function declaration
                {
                    Regex = new Regex(@"^(public|stock|forward)\s+[a-zA-z_][a-zA-z1-9_]*:",
                        RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.ExplicitCapture),
                    Color = new HighlightingColor { Foreground = deprecatedBrush }
                });
                rs.Rules.Add(new HighlightingRule // deprecated taggings (from std types)
                {
                    Regex = new Regex(@"\b(bool|Float|float|Handle|String|char|void|int):",
                        RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.ExplicitCapture),
                    Color = new HighlightingColor { Foreground = deprecatedBrush }
                });
                rs.Rules.Add(new HighlightingRule // deprecated keywords
                {
                    Regex = RegexKeywordsHelper.GetRegexFromKeywords(new[]
                        {"decl", "String", "Float", "functag", "funcenum"}),
                    Color = new HighlightingColor { Foreground = deprecatedBrush }
                });
            }

            // Apply normal stock rules

            rs.Rules.Add(new HighlightingRule // preprocessor keywords
            {
                Regex = new Regex(@"\#\S+",
                    RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture),
                Color = new HighlightingColor { Foreground = preprocessorBrush }
            });
            rs.Rules.Add(new HighlightingRule // #pragma deprecated messages
            {
                Regex = new Regex(@"(?<=#pragma deprecated ).+", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture),
                Color = new HighlightingColor { Foreground = stringBrush }
            });
            rs.Rules.Add(new HighlightingRule // type-values keywords
            {
                Regex = RegexKeywordsHelper.GetRegexFromKeywords(new[] { "sizeof", "true", "false", "null" }),
                Color = new HighlightingColor { Foreground = typeValuesBrush }
            });
            rs.Rules.Add(new HighlightingRule // main keywords
            {
                Regex = RegexKeywordsHelper.GetRegexFromKeywords(new[]
                {
                    "if", "else", "switch", "case", "default", "for", "while", "do", "break", "continue", "return",
                    "new", "view_as", "delete"
                }),
                Color = new HighlightingColor { Foreground = mainKeywordsBrush }
            });
            
            rs.Rules.Add(new HighlightingRule // context keywords
            {
                Regex = RegexKeywordsHelper.GetRegexFromKeywords(new[]
                {
                    "stock", "normal", "native", "public", "static", "const", "methodmap", "enum", "forward",
                    "function", "struct", "property", "get", "set", "typeset", "typedef", "this", "operator"
                }),
                Color = new HighlightingColor { Foreground = contextKeywordsBrush }
            });

            rs.Rules.Add(new HighlightingRule // value types
            {
                Regex = RegexKeywordsHelper.GetRegexFromKeywords(new[]
                    {"bool", "char", "float", "int", "void", "any", "Handle", "Function"}),
                Color = new HighlightingColor { Foreground = typesBrush }
            });
            rs.Rules.Add(new HighlightingRule // char type
            {
                Regex = new Regex(@"'\\?.?'",
                    RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture),
                Color = new HighlightingColor { Foreground = charBrush }
            });
            rs.Rules.Add(new HighlightingRule // numbers
            {
                Regex = new Regex(
                    @"\b0[x][0-9a-fA-F]+|\b0[b][01]+|\b0[o][0-7]+|([+-]?\b[0-9]+(\.[0-9]+)?|\.[0-9]+)([eE][+-]?[0-9]+)?",
                    RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture),
                Color = new HighlightingColor { Foreground = numberBrush }
            });
            rs.Rules.Add(new HighlightingRule // special characters
            {
                Regex = new Regex(@"[?.;()\[\]{}+\-/%*&<>^+~!|&]+",
                    RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture),
                Color = new HighlightingColor { Foreground = specialCharBrush }
            });
            rs.Rules.Add(new HighlightingRule // std includes - string color!
            {
                Regex = new Regex(@"\s[<][\w\\/\-]+(\.[\w\-]+)?[>]",
                    RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture),
                Color = new HighlightingColor { Foreground = stringBrush }
            });

            // Apply particular rules from the current SM Definition
            var def = smDef ?? Program.Configs[Program.SelectedConfig].GetSMDef();

            if (def.Defines.Count > 0)
            {
                rs.Rules.Add(new HighlightingRule // defines
                {
                    Regex = new Regex("\\b(?:" + string.Join("|", def.Defines.Select(x => Regex.Match(x.Name, @"\w+").Value).ToArray()) + ")(?=\\W|$)"),
                    Color = new HighlightingColor { Foreground = constantBrush }
                });
            }
            
            if (def.TypeStrings.Count > 0)
            {
                rs.Rules.Add(new HighlightingRule // types
                {
                    Regex = RegexKeywordsHelper.GetRegexFromKeywords(def.TypeStrings),
                    Color = new HighlightingColor { Foreground = typesBrush }
                });
            }

            if (def.Constants.Count > 0)
            {
                rs.Rules.Add(new HighlightingRule // constants
                {
                    Regex = RegexKeywordsHelper.GetRegexFromKeywords(def.Constants),
                    Color = new HighlightingColor { Foreground = constantBrush }
                });
            }

            if (def.FunctionStrings.Length > 0)
            {
                rs.Rules.Add(new HighlightingRule // Functions
                {
                    Regex = RegexKeywordsHelper.GetFunctionRegex(def.FunctionStrings),
                    Color = new HighlightingColor { Foreground = functionBrush }
                });
            }

            if (def.ObjectMethods.Count > 0)
            {
                rs.Rules.Add(new HighlightingRule // Methods
                {
                    Regex = RegexKeywordsHelper.GetMethodRegex(def.ObjectMethods),
                    Color = new HighlightingColor { Foreground = methodBrush }
                });
            }
            
            if (def.ObjectFields.Count > 0)
            {
                rs.Rules.Add(new HighlightingRule // Methods
                {
                    Regex = RegexKeywordsHelper.GetMethodRegex(def.ObjectFields),
                    Color = new HighlightingColor { Foreground = methodBrush }
                });
            }

            // The unknown function calls rule is at the end because it gets applied after parsing all of the known functions.

            rs.Rules.Add(new HighlightingRule // unknown function calls
            {
                //Regex = new Regex(@"\b\w+(?=\s*\()",
                Regex = new Regex(@"(?<!#define )\b\w+(?=\s*\()",
                    RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture),
                Color = new HighlightingColor { Foreground = unknownFunctionBrush }
            });

            rs.Name = "MainRule";
            return rs;
        }
    }

    public HighlightingRuleSet GetNamedRuleSet(string name)
    {
        return null;
    }

    public HighlightingColor GetNamedColor(string name)
    {
        return null;
    }

    public IEnumerable<HighlightingColor> NamedHighlightingColors { get; set; }

    public IDictionary<string, string> Properties
    {
        get
        {
            var propertiesDictionary = new Dictionary<string, string>
            {
                { "DocCommentMarker", "///" }
            };
            return propertiesDictionary;
        }
    }
}

[Serializable]
public sealed class SimpleHighlightingBrush : HighlightingBrush, ISerializable
{
    private readonly SolidColorBrush brush;

    internal SimpleHighlightingBrush(SolidColorBrush brush)
    {
        brush.Freeze();
        this.brush = brush;
    }

    public SimpleHighlightingBrush(Color color) : this(new SolidColorBrush(color))
    {
    }

    private SimpleHighlightingBrush(SerializationInfo info, StreamingContext context)
    {
        brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(info.GetString("color")));
        brush.Freeze();
    }

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("color", brush.Color.ToString(CultureInfo.InvariantCulture));
    }

    public override Brush GetBrush(ITextRunConstructionContext context)
    {
        return brush;
    }

    public override string ToString()
    {
        return brush.ToString();
    }

    public override bool Equals(object obj)
    {
        if (obj is not SimpleHighlightingBrush other)
        {
            return false;
        }

        return brush.Color.Equals(other.brush.Color);
    }

    public override int GetHashCode()
    {
        return brush.Color.GetHashCode();
    }
}