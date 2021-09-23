using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using EnumExtension;
using SourcepawnCondenser.SourcemodDefinition;
using SourcepawnCondenser.Tokenizer;

namespace SourcepawnCondenser
{
    public class SPFunctionConsumerNew : SourcePawnConsumer
    {
        public SPFunctionConsumerNew(ImmutableList<Token> tokens, string fileName) : base(tokens, fileName)
        {
        }

        // TODO: fullname should be updated.
        public override (SMBaseDefinition? def, int count) Consume(int position)
        {
            var tokens = Tokens.GetRange(position, Tokens.Count - position);

            // A function must have at least 4 tokens
            if (tokens.Count < 4)
            {
                return (null, 1);
            }

            var functionName = string.Empty;
            var functionReturnType = string.Empty;

            // Match the function kind.
            var kind = _extractKind(tokens, position);

            //  "public native" and "stock static" functions require two tokens.
            var index = kind switch
            {
                SMFunctionKind.PublicNative or SMFunctionKind.StockStatic => 2,
                SMFunctionKind.None => 0,
                _ => 1
            };


            // find the comment
            var commentString = FindComment(position);


            // Find the function return type
            if (tokens[index].Kind != TokenKind.Identifier)
            {
                return InvalidValue;
            }

            var returnType = tokens[index].Value;
            if (IsArray(index + 1))
            {
                returnType += "[]";
                index += 2;
            }

            // Find the function name
            if (tokens[index].Kind != TokenKind.Identifier)
            {
                return InvalidValue;
            }

            var funcName = tokens[index++].Value;
            if (tokens[index].Kind != TokenKind.ParenthesisOpen)
            {
                return InvalidValue;
            }

            // Extract all the function parameters name.
            var endParamsIndex = FortraceTestForToken(index, TokenKind.ParenthesisClose, true, true);
            if (endParamsIndex == -1)
            {
                return InvalidValue;
            }

            var parametersTokens = tokens.GetRange(index + 1, endParamsIndex - 1);
            var parameters = parametersTokens.Split((token) => token.Kind == TokenKind.Comma, true).ToList();
            var functionParameters = parameters.Select(paramTokens => paramTokens.Last())
                .Where(nameToken =>
                    nameToken.Kind == TokenKind.Identifier && !string.IsNullOrWhiteSpace(nameToken.Value))
                .Select(nameToken => nameToken.Value).ToList();

            index = endParamsIndex + 1;

            // function declared as "native void function();", native functions are the only function type that allow that.
            if (tokens[index].Kind == TokenKind.Semicolon)
            {
                // TODO: Complete `fullName` by adding the full function definition.
                return (new SMFunction(
                    tokens, index, FileName, funcName, commentString, funcName, returnType,
                    functionParameters.ToImmutableList(), kind,
                    (new List<SMVariable>()).ToImmutableList()), index);
            }

            if (tokens[index].Kind != TokenKind.BraceOpen || tokens.Count < index + 1)
            {
                //TODO: Maybe this should return an InvalidValue.
                return (new SMFunction(
                    tokens, index, FileName, funcName, commentString, funcName, returnType,
                    functionParameters.ToImmutableList(), kind,
                    (new List<SMVariable>()).ToImmutableList()), index);
            }


            var braceState = 0;

            //TODO: Come back here to add local variables & scopes support.
            for (var i = index; i < tokens.Count; ++i)
            {
                if (tokens[i].Kind == TokenKind.BraceOpen)
                {
                    ++braceState;
                }
                else if (tokens[i].Kind == TokenKind.BraceClose)
                {
                    --braceState;
                    if (braceState == 0)
                    {
                        return (new SMFunction(
                            tokens, index, FileName, funcName, commentString, funcName, returnType,
                            functionParameters.ToImmutableList(), kind,
                            (new List<SMVariable>()).ToImmutableList()), i);
                        // localVars = LocalVars.ConsumeSMVariableLocal(
                            // _tokens.GetRange(nextOpenBraceTokenIndex, i - nextOpenBraceTokenIndex), _fileName);
                    }
                }
            }


            return (new SMFunction(
                tokens, index, FileName, funcName, commentString, funcName, returnType,
                functionParameters.ToImmutableList(), kind,
                (new List<SMVariable>()).ToImmutableList()), index);
        }

        private static SMFunctionKind _extractKind(IImmutableList<Token> tokens, int position)
        {
            // Match the function type
            switch (tokens.First().Value)
            {
                case "stock":
                    {
                        if (tokens[1].Kind == TokenKind.FunctionIndicator && tokens[1].Value == "static")
                        {
                            return SMFunctionKind.StockStatic;
                        }

                        return SMFunctionKind.Stock;
                    }
                case "native":
                    {
                        return SMFunctionKind.Native;
                    }
                case "forward":
                    {
                        return SMFunctionKind.Forward;
                    }
                case "public":
                    {
                        if (tokens[1].Kind == TokenKind.FunctionIndicator && tokens[1].Value == "native")
                        {
                            {
                                return SMFunctionKind.PublicNative;
                            }
                        }

                        return SMFunctionKind.Public;
                    }
                case "static":
                    {
                        return SMFunctionKind.Static;
                    }
                case "normal":
                    {
                        return SMFunctionKind.Normal;
                    }
                default:
                    {
                        return SMFunctionKind.None;
                    }
            }
        }
    }
}