using Godot;
using System;

namespace Fractural.NodeVars
{
    public static class ExpressionUtils
    {
        public static ExpressionLexer Lexer { get; } = new ExpressionLexer();
        public static ExpressionParser Parser { get; } = new ExpressionParser();
        public static ExpressionParser.Expression ParseFromText(string text, ExpressionParser.Variable.GetVariableDelegate getVariableFunc = null, ExpressionParser.FunctionCall.CallFunctionDelegate callFunctionFunc = null)
        {
            var tokens = Lexer.Tokenize(text);
            if (tokens == null)
            {
                throw new Exception($"{nameof(ParseFromText)}: Could not tokenize the text.");
            }
            var ast = Parser.Parse(tokens, getVariableFunc, callFunctionFunc);
            if (ast == null)
            {
                throw new Exception($"{nameof(ParseFromText)}: Could not parse the tokens.");
            }
            return ast;
        }

        public static object EvaluateFromText(string text, ExpressionParser.Variable.GetVariableDelegate fetchVariableFunc = null, ExpressionParser.FunctionCall.CallFunctionDelegate callFunctionFunc = null)
        {
            var ast = ParseFromText(text, fetchVariableFunc, callFunctionFunc);
            if (ast == null) return null;
            return ast.Evaluate();
        }
    }
}