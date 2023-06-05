using Fractural.NodeVars;
using Fractural.Utils;
using System;

namespace Tests
{
    public class ExpressionEvaluationTests : WAT.Test
    {
        [Test("(5 + 3 / 3 * 4 - 2) * 3", 21)]
        [Test("true && false || true", true)]
        [Test("\"hello\" + \" world\"", "hello world")]
        public void TestEvaluation(string str, object result)
        {
            Describe($"When evaluating \"{str}\"");
            ExpressionLexer lexer = new ExpressionLexer();
            var tokens = lexer.Tokenize(str);
            Assert.IsNotNull(tokens, "Should succesfully tokenize");
            if (tokens == null)
                return;
            ExpressionParser parser = new ExpressionParser();
            var ast = parser.Parse(tokens, null, null);
            Assert.IsNotNull(ast, "Should succesfully parse");
            if (ast == null)
                return;
            Assert.IsEqual(ast.Evaluate(), result, "Should evaluate to the correct result");
        }

        [Test("myAdd(3, myVar)", 8)]
        [Test("myAdd(-3, myVar)", 2)]
        [Test("isNegative(myAdd(-3, myVar))", false)]
        public void TestVarAndFuncEvaluation(string str, object result)
        {
            Describe($"When evaluating \"{str}\" with funcs");
            ExpressionLexer lexer = new ExpressionLexer();
            var tokens = lexer.Tokenize(str);
            Assert.IsNotNull(tokens, "Should succesfully tokenize");
            if (tokens == null)
                return;
            ExpressionParser parser = new ExpressionParser();
            var ast = parser.Parse(tokens,
                (varName) =>
                {
                    switch (varName)
                    {
                        case "myVar":
                            return 5;
                    }
                    return null;
                },
                (funcName, args) =>
                {
                    switch (funcName)
                    {
                        case "myAdd":
                            return args.ElementAt<int>(0) + args.ElementAt<int>(1);
                        case "isNegative":
                            return args.ElementAt<int>(0) < 0;
                    }
                    return null;
                }
            );
            Assert.IsNotNull(ast, "Should succesfully parse");
            if (ast == null)
                return;
            Assert.IsEqual(ast.Evaluate(), result, "Should evaluate to the correct result");
        }
    }
}