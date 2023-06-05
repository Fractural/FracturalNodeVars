using Fractural.Utils;
using Godot;
using System;
using System.Collections.Generic;

namespace Fractural.NodeVars
{
    /// <summary>
    /// Simple expression lexer that converts text to tokens 
    /// that are parsable by the <seealso cref="ExpressionParser"/>.
    /// </summary>
    public class ExpressionLexer
    {
        public enum TokenType
        {
            String,
            Number,
            Identifier,
            Keyword,
            Punctuation,
        }

        public class Token
        {
            public object Value { get; set; }
            public TokenType TokenType { get; set; }

            public override bool Equals(object obj)
            {
                return obj is Token token &&
                    Equals(token.Value, Value) &&
                    Equals(token.TokenType, TokenType);
            }

            public override int GetHashCode()
            {
                int code = Value?.GetHashCode() ?? 0;
                code = GeneralUtils.CombineHashCodes(code, TokenType.GetHashCode());
                return code;
            }
        }

        private int _index;
        private string _text;
        private List<Token> _tokens;
        private string[] _keywords;
        private string[] _punctuation;
        private IDictionary<char, string> _escapeSequences;
        private string[] DefaultKeywords => new string[] {
            "true",
            "false",
            "and",
            "or"
        };
        private string[] DefaultPunctuation => new string[] {
            "!",
            "+",
            "-",
            "/",
            "*",
            "(",
            ")",
            "==",
            ">=",
            "<=",
            ">",
            "<",
            "&&",
            "||",
            ","
        };
        private IDictionary<char, string> DefaultEscapeSequences => new Dictionary<char, string>()
        {
            {'t', "\t" },
            {'n', "\n" },
            {'"', "\"" },
            {'\'', "'" },
            {'\\', "\\" },
            {'0', "\0" },
            {'b', "\b" },
            {'v', "\v" },
        };

        public bool IsEOF()
        {
            return _index >= _text.Length;
        }

        public char NextChar()
        {
            if (_index >= _text.Length)
                return default;
            return _text[_index++];
        }

        public char PeekChar(int offset = 0)
        {
            if ((_index + offset) >= _text.Length)
                return default;
            return _text[_index + offset];
        }

        public bool IsValidIdentifierStart(char c)
        {
            int lowerCharIdx = (int)char.ToLower(c);
            return 'a' <= lowerCharIdx && 'z' >= lowerCharIdx || c == '_';
        }

        public bool IsValidIdentifierChar(char c)
        {
            int lowerCharIdx = (int)char.ToLower(c);
            return ('a' <= lowerCharIdx && 'z' >= lowerCharIdx) || c == '_' || char.IsDigit(c);
        }

        public void ConsumeWhitespace()
        {
            while (char.IsWhiteSpace(PeekChar()))
                NextChar();
        }

        public bool ExpectString(string expected)
        {
            if (_index + expected.Length > _text.Length)
                return false;
            if (_text.Substring(_index, expected.Length).Equals(expected))
            {
                _index += expected.Length;
                return true;
            }
            return false;
        }

        public Token ExpectIdentifier()
        {
            var name = "";
            if (!IsValidIdentifierStart(PeekChar())) return null;
            name += NextChar();
            while (IsValidIdentifierChar(PeekChar()))
                name += NextChar();
            return new Token() { Value = name, TokenType = TokenType.Identifier };
        }

        public Token ExpectPunctuation()
        {
            foreach (string punctuation in _punctuation)
                if (ExpectString(punctuation))
                    return new Token() { TokenType = TokenType.Punctuation, Value = punctuation };
            return null;
        }

        public Token ExpectKeyword()
        {
            foreach (string keyword in _keywords)
                if (ExpectString(keyword))
                    return new Token() { TokenType = TokenType.Keyword, Value = keyword };
            return null;
        }

        public Token ExpectNumberLiteral()
        {
            string numberLiteralText = "";
            int index = 0;
            bool foundDecimal = false;
            while (char.IsDigit(PeekChar(index)) || PeekChar(index) == '.')
            {
                var nextChar = NextChar();
                if (nextChar == '.')
                {
                    // We can't have two decimals!
                    if (foundDecimal)
                        return null;
                    foundDecimal = true;
                }
                numberLiteralText += nextChar;
            }
            if (foundDecimal)
            {
                if (float.TryParse(numberLiteralText, out float result))
                    return new Token() { Value = result, TokenType = TokenType.Number };
            }
            else
            {
                if (int.TryParse(numberLiteralText, out int result))
                    return new Token() { Value = result, TokenType = TokenType.Number };
            }
            return null;
        }

        public Token ExpectStringLiteral()
        {
            if (PeekChar() != '"') return null;
            NextChar();
            string resultString = "";
            while (PeekChar() != '"')
            {
                var nextChar = NextChar();
                if (nextChar == '\\')
                {
                    if (IsEOF())
                        // Unterminated string
                        return null;
                    var escapeSequenceChar = NextChar();
                    if (_escapeSequences.TryGetValue(escapeSequenceChar, out string escapedValue))
                        resultString += escapedValue;
                    else
                        // Unknown escape sequence
                        return null;
                }
                else
                    resultString += nextChar;
                if (IsEOF())
                    // Unterminated string
                    return null;
            }
            NextChar();
            return new Token() { Value = resultString, TokenType = TokenType.String };
        }

        public Token ExpectToken()
        {
            var token = ExpectKeyword();
            if (token != null) return token;
            token = ExpectPunctuation();
            if (token != null) return token;
            token = ExpectIdentifier();
            if (token != null) return token;
            token = ExpectNumberLiteral();
            if (token != null) return token;
            token = ExpectStringLiteral();
            return token;
        }

        public string PeekString(int amount)
        {
            int substringLength = amount;
            if (_text.Length - _index < substringLength)
                substringLength = _text.Length - _index;
            return _text.Substring(_index, substringLength);
        }

        public IList<Token> Tokenize(string text, string[] keywords = null, string[] punctuation = null, IDictionary<char, string> escapeSequences = null)
        {
            _text = text;
            _index = 0;
            _tokens = new List<Token>();

            _keywords = keywords ?? DefaultKeywords;
            _punctuation = punctuation ?? DefaultPunctuation;
            _escapeSequences = escapeSequences ?? DefaultEscapeSequences;

            while (!IsEOF())
            {
                var token = ExpectToken();
                if (token == null)
                {
                    GD.PushError($"{nameof(ExpressionLexer)}: Unknown token \"{PeekString(10)}\".");
                    return null;
                }
                _tokens.Add(token);
                ConsumeWhitespace();
            }
            return _tokens;
        }
    }
}