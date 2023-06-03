using Fractural.NodeVars;
using Godot;
using static Fractural.NodeVars.ExpressionParser;
using Expression = Fractural.NodeVars.ExpressionParser.Expression;

namespace Tests
{
    public class ExpressionParserTests : WAT.Test
    {
        private void TestParse(string str, Expression expectedAST)
        {
            Describe($"When parsing \"{str}\"");
            ExpressionLexer lexer = new ExpressionLexer();
            var tokens = lexer.Tokenize(str);
            Assert.IsNotNull(tokens, "Should succesfully tokenize");
            if (tokens == null)
                return;
            ExpressionParser parser = new ExpressionParser();
            var ast = parser.Parse(tokens, null, null);
            Assert.IsEqual(ast, expectedAST, "Should create the correct AST");
        }

        [Test]
        public void TestInt() => TestParse(
           "324",
           new Literal()
           {
               Value = 324
           });

        [Test]
        public void TestFloat() => TestParse(
           "324.349",
           new Literal()
           {
               Value = 324.349f
           });

        [Test]
        public void TestString() => TestParse(
           "\"This is a string\"",
           new Literal()
           {
               Value = "This is a string"
           });

        [Test]
        public void TestIdentifier() => TestParse(
           "myCoolVar",
           new Variable()
           {
               FetchVariable = null,
               Name = "myCoolVar"
           });

        [Test]
        public void TestFunction() => TestParse(
           "myCoolFunc()",
           new FunctionCall()
           {
               CallFunction = null,
               Name = "myCoolFunc",
               Args = new Expression[0]
           });

        [Test]
        public void TestFunctionWithOneArg() => TestParse(
           "myCoolFunc(234.34)",
           new FunctionCall()
           {
               CallFunction = null,
               Name = "myCoolFunc",
               Args = new Expression[]
               {
                   new Literal() { Value = 234.34f }
               }
           });

        [Test]
        public void TestFunctionWithTwoArgs() => TestParse(
           "myCoolFunc(234.34, 45 - 6)",
           new FunctionCall()
           {
               CallFunction = null,
               Name = "myCoolFunc",
               Args = new Expression[]
               {
                   new Literal() { Value = 234.34f },
                   new SubtractOperator()
                   {
                       LeftOperand = new Literal() { Value = 45 },
                       RightOperand = new Literal() { Value = 6 }
                   }
               }
           });

        [Test]
        public void TestNumberExpression() => TestParse(
           "5 + (6 * myVar - 2) * 3",
           new AddOperator()
           {
               LeftOperand = new Literal() { Value = 5 },
               RightOperand = new MultiplyOperator()
               {
                   LeftOperand = new SubtractOperator()
                   {
                       LeftOperand = new MultiplyOperator()
                       {
                           LeftOperand = new Literal() { Value = 6 },
                           RightOperand = new Variable() { Name = "myVar" }
                       },
                       RightOperand = new Literal() { Value = 2 }
                   },
                   RightOperand = new Literal() { Value = 3 }
               }
           });

        [Test]
        public void TestBoolExpression() => TestParse(
           "true && 3 > 5 + 6 || false",
           new AndOperator()
           {
               LeftOperand = new Literal() { Value = true },
               RightOperand = new GreaterThanOperator()
               {
                   LeftOperand = new Literal() { Value = 3 },
                   RightOperand = new OrOperator()
                   {
                       LeftOperand = new AddOperator()
                       {
                           LeftOperand = new Literal() { Value = 5 },
                           RightOperand = new Literal() { Value = 6 }
                       },
                       RightOperand = new Literal() { Value = false }
                   }
               },
           });

        [Test]
        public void TestPreunaryExpession() => TestParse(
            "-5 || !false",
            new OrOperator()
            {
                LeftOperand = new NegativeOperator()
                {
                    Operand = new Literal() { Value = 5 }
                },
                RightOperand = new NegationOperator()
                {
                    Operand = new Literal() { Value = false }
                }
            });

        [Test]
        public void TestSamePrecedenceNumberExpession() => TestParse(
            "5 / 6 * 5 / 3",
            new DivideOperator()
            {
                LeftOperand = new MultiplyOperator()
                {
                    LeftOperand = new DivideOperator()
                    {
                        LeftOperand = new Literal() { Value = 5 },
                        RightOperand = new Literal() { Value = 6 }
                    },
                    RightOperand = new Literal() { Value = 5 }
                },
                RightOperand = new Literal() { Value = 3 }
            });
    }
}