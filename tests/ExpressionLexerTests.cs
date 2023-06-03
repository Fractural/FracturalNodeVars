using Fractural.NodeVars;
using static Fractural.NodeVars.ExpressionLexer;

namespace Tests
{
    public class ExpressionLexerTests : WAT.Test
    {
        private void TestTokenize(string str, Token[] expectedTokens)
        {
            Describe($"When tokenizing \"{str}\"");
            ExpressionLexer lexer = new ExpressionLexer();
            var tokens = lexer.Tokenize(str);
            Assert.IsEqual(tokens.Count, expectedTokens.Length, "Should create the correct number of tokens");
            if (tokens.Count == expectedTokens.Length)
                for (int i = 0; i < expectedTokens.Length; i++)
                    Assert.IsEqual(tokens[i], expectedTokens[i], $"Should create the correct token[{i}]");
        }

        [Test(".")]
        [Test("44.23 ;")]
        [Test("3934.2349.334")]
        public void TestUntokenizableExpression(string expression)
        {
            Describe($"When tokenizing \"{expression}\"");
            ExpressionLexer lexer = new ExpressionLexer();
            var tokens = lexer.Tokenize(expression);
            Assert.IsNull(tokens, "Should fail on tokenizing");
        }

        [Test]
        public void TestInt() => TestTokenize(
           "324",
           new Token[]
           {
               new Token()
               {
                   TokenType = TokenType.Number,
                   Value = 324
               }
           });

        [Test]
        public void TestFloat() => TestTokenize(
           "324.349",
           new Token[]
           {
               new Token()
               {
                   TokenType = TokenType.Number,
                   Value = 324.349f
               }
           });

        [Test]
        public void TestIdentifier() => TestTokenize(
           "myCoolVar",
           new Token[]
           {
               new Token()
               {
                   TokenType = TokenType.Identifier,
                   Value = "myCoolVar"
               }
           });

        [Test]
        public void TestString() => TestTokenize(
           "\"my awesome string\"",
           new Token[]
           {
               new Token()
               {
                   TokenType = TokenType.String,
                   Value = "my awesome string"
               }
           });

        [Test]
        public void TestStringWithEscapeChars() => TestTokenize(
           @"""\t \n \\ \"" '""",
           new Token[]
           {
               new Token()
               {
                   TokenType = TokenType.String,
                   Value = "\t \n \\ \" '"
               }
           });

        [Test]
        public void TestNumberExpression() => TestTokenize(
             "0 + (5 - 10.34) * .23 - 19.",
             new Token[]
             {
                new Token()
                {
                    TokenType = TokenType.Number,
                    Value = 0
                },
                new Token()
                {
                    TokenType = TokenType.Punctuation,
                    Value = "+"
                },
                new Token()
                {
                    TokenType = TokenType.Punctuation,
                    Value = "("
                },
                new Token()
                {
                    TokenType = TokenType.Number,
                    Value = 5
                },
                new Token()
                {
                    TokenType = TokenType.Punctuation,
                    Value = "-"
                },
                new Token()
                {
                    TokenType = TokenType.Number,
                    Value = 10.34f
                },
                new Token()
                {
                    TokenType = TokenType.Punctuation,
                    Value = ")"
                },
                new Token()
                {
                    TokenType = TokenType.Punctuation,
                    Value = "*"
                },
                new Token()
                {
                    TokenType = TokenType.Number,
                    Value = 0.23f
                },
                new Token()
                {
                    TokenType = TokenType.Punctuation,
                    Value = "-"
                },
                new Token()
                {
                    TokenType = TokenType.Number,
                    Value = 19f
                },
             });

        [Test]
        public void TestBoolExpression() => TestTokenize(
            "true && (false || false or true)",
            new Token[]
            {
                new Token()
                {
                    TokenType = TokenType.Keyword,
                    Value = "true"
                },
                new Token()
                {
                    TokenType = TokenType.Punctuation,
                    Value = "&&"
                },
                new Token()
                {
                    TokenType = TokenType.Punctuation,
                    Value = "("
                },
                new Token()
                {
                    TokenType = TokenType.Keyword,
                    Value = "false"
                },
                new Token()
                {
                    TokenType = TokenType.Punctuation,
                    Value = "||"
                },
                new Token()
                {
                    TokenType = TokenType.Keyword,
                    Value = "false"
                },
                new Token()
                {
                    TokenType = TokenType.Keyword,
                    Value = "or"
                },
                new Token()
                {
                    TokenType = TokenType.Keyword,
                    Value = "true"
                },
                new Token()
                {
                    TokenType = TokenType.Punctuation,
                    Value = ")"
                },
            });

        [Test]
        public void TestVariableExpression() => TestTokenize(
            "myVar + \"cool string 48.3848\" * _otherVar",
            new Token[]
            {
                new Token()
                {
                    TokenType = TokenType.Identifier,
                    Value = "myVar"
                },
                new Token()
                {
                    TokenType = TokenType.Punctuation,
                    Value = "+"
                },
                new Token()
                {
                    TokenType = TokenType.String,
                    Value = "cool string 48.3848"
                },
                new Token()
                {
                    TokenType = TokenType.Punctuation,
                    Value = "*"
                },
                new Token()
                {
                    TokenType = TokenType.Identifier,
                    Value = "_otherVar"
                },
            });

        [Test]
        public void TestFunctionExpression() => TestTokenize(
            "myFunc(34.34) - otherFunc(thisVar, myVar * _otherVar)",
            new Token[]
            {
                new Token()
                {
                    TokenType = TokenType.Identifier,
                    Value = "myFunc"
                },
                new Token()
                {
                    TokenType = TokenType.Punctuation,
                    Value = "("
                },
                new Token()
                {
                    TokenType = TokenType.Number,
                    Value = 34.34f
                },
                new Token()
                {
                    TokenType = TokenType.Punctuation,
                    Value = ")"
                },
                new Token()
                {
                    TokenType = TokenType.Punctuation,
                    Value = "-"
                },
                new Token()
                {
                    TokenType = TokenType.Identifier,
                    Value = "otherFunc"
                },
                new Token()
                {
                    TokenType = TokenType.Punctuation,
                    Value = "("
                },
                new Token()
                {
                    TokenType = TokenType.Identifier,
                    Value = "thisVar"
                },
                new Token()
                {
                    TokenType = TokenType.Punctuation,
                    Value = ","
                },
                new Token()
                {
                    TokenType = TokenType.Identifier,
                    Value = "myVar"
                },
                new Token()
                {
                    TokenType = TokenType.Punctuation,
                    Value = "*"
                },
                new Token()
                {
                    TokenType = TokenType.Identifier,
                    Value = "_otherVar"
                },
                new Token()
                {
                    TokenType = TokenType.Punctuation,
                    Value = ")"
                },
            });
    }
}