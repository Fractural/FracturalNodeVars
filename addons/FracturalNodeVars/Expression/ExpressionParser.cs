using Fractural.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fractural.NodeVars
{
    /// <summary>
    /// Simple expression parser that does arithmetic, boolean, and equality operations. 
    /// Uses int, float, bool, and string types.
    /// </summary>
    public class ExpressionParser
    {
        #region AST Nodes
        public abstract class Expression
        {
            protected class ASTStringBuilder
            {
                public StringBuilder Builder { get; set; } = new StringBuilder();
                public string IndentString { get; set; } = "  ";
                public int IndentCount { get; private set; }

                private bool _isPrevWriteField = false;
                private int _disableWriteStartIndentsCount = 0;

                public void Indent() => IndentCount++;
                public void Dedent()
                {
                    if (IndentCount > 0)
                        IndentCount--;
                    else
                        throw new Exception("Dedented more than indent amount!");
                }

                public void WriteField(string name, Expression expression)
                {
                    Write(name);
                    Builder.Append(": ");
                    if (expression != null)
                    {
                        _disableWriteStartIndentsCount = 1;
                        expression.BuildString(this);
                        _disableWriteStartIndentsCount = 0;
                    }
                    else
                        Builder.Append("null");
                    Builder.Append(",\n");
                }

                public void WriteField(string name, object value)
                {
                    Write(name);
                    Builder.Append(": ");
                    Builder.Append(value ?? "null");
                    Builder.Append(",\n");
                }

                public void WriteFieldArray(string name, IEnumerable<Expression> expressions)
                {
                    Write(name);
                    Builder.Append(":");
                    WriteBlock(() =>
                    {
                        foreach (var expression in expressions)
                        {
                            expression.BuildString(this);
                            Builder.Append(",\n");
                        }
                    }, "[", "]");
                    Builder.Append(",\n");
                }

                public void WriteFieldArray(string name, IEnumerable<string> values)
                {
                    Write(name);
                    Builder.Append(":");
                    WriteBlock(() =>
                    {
                        foreach (var value in values)
                        {
                            Write(name);
                            Builder.Append(",\n");
                        }
                    }, "[", "]");
                    Builder.Append(",\n");
                }

                public void WriteBlock(string name, Action write, string startBlockStr = "{", string endBlockStr = "}")
                {
                    Write(name);
                    Builder.Append(": ");
                    WriteBlock(write, startBlockStr, endBlockStr, false);
                }

                public void WriteWhitespaceBlock(Action write, bool indentStart = true) => WriteBlock(write, null, null, indentStart);

                public void WriteBlock(Action write, string startBlockStr = "{", string endBlockStr = "}", bool indentStart = true)
                {
                    if (startBlockStr != null)
                        startBlockStr = "";
                    WriteLine(startBlockStr, indentStart);
                    Indent();
                    write();

                    // Remove the extra comma if this is the last field.
                    var prevFieldStringLength = (2 + IndentCount * IndentString.Length);
                    var prevFieldStringStartIndex = Builder.Length - prevFieldStringLength;
                    if (prevFieldStringStartIndex >= 0 && prevFieldStringLength > Builder.Length &&
                        Builder.ToString(prevFieldStringStartIndex, 2).Equals(",\n"))
                        Builder.Remove(prevFieldStringStartIndex, prevFieldStringLength);

                    Dedent();
                    if (endBlockStr != null && endBlockStr != "")
                        Write(endBlockStr);
                }

                public void Write(string text, bool indent = true)
                {
                    if (indent)
                    {
                        if (_disableWriteStartIndentsCount > 0)
                            _disableWriteStartIndentsCount--;
                        else
                            for (int i = 0; i < IndentCount; i++)
                                Builder.Append(IndentString);
                    }
                    Builder.Append(text);
                }

                public void WriteLine(string text, bool indent = true)
                {
                    if (indent)
                    {
                        if (_disableWriteStartIndentsCount > 0)
                            _disableWriteStartIndentsCount--;
                        else
                            for (int i = 0; i < IndentCount; i++)
                                Builder.Append(IndentString);
                    }
                    Builder.AppendLine(text);
                }

                public override string ToString() => Builder.ToString();
            }

            public abstract object Evaluate();
            public override string ToString()
            {
                var builder = new ASTStringBuilder();
                BuildString(builder);
                return builder.ToString();
            }
            protected abstract void BuildString(ASTStringBuilder builder);
        }

        public class Variable : Expression
        {
            public delegate object FetchVariableDelegate(string name);
            public FetchVariableDelegate FetchVariable { get; set; }
            public string Name { get; set; }
            public override object Evaluate()
            {
                if (FetchVariable == null)
                {
                    GD.PushError($"{nameof(Variable)}: Expected FetchVariable to be assigned before evaluating the variable in an expression AST!");
                    return null;
                }
                return FetchVariable(Name);
            }
            public override bool Equals(object obj)
            {
                return obj is Variable variable &&
                    Equals(Name, variable.Name) &&
                    Equals(FetchVariable, variable.FetchVariable);
            }
            public override int GetHashCode()
            {
                int code = Name.GetHashCode();
                code = GeneralUtils.CombineHashCodes(code, FetchVariable?.GetHashCode() ?? 0);
                return code;
            }
            protected override void BuildString(ASTStringBuilder builder)
            {
                builder.WriteBlock(nameof(Variable), () =>
                {
                    builder.WriteField(nameof(Name), Name);
                });
            }
        }

        public class FunctionCall : Expression
        {
            public delegate object CallFunctionDelegate(string name, object[] args);
            public CallFunctionDelegate CallFunction { get; set; }
            public string Name { get; set; }
            public Expression[] Args { get; set; }
            public override object Evaluate()
            {
                if (CallFunction == null)
                {
                    GD.PushError($"{nameof(FunctionCall)}: Expected CallFunction to be assigned before running a function call in an expression AST!");
                    return null;
                }
                var evalutedArgs = Args.Select(x => x.Evaluate()).ToArray();
                return CallFunction(Name, evalutedArgs);
            }
            public override bool Equals(object obj)
            {
                return obj is FunctionCall functionCall &&
                    Equals(functionCall.Name, Name) &&
                    Args.SequenceEqual(functionCall.Args);
            }
            public override int GetHashCode()
            {
                int code = Name.GetHashCode();
                foreach (var arg in Args)
                    code = GeneralUtils.CombineHashCodes(code, arg.GetHashCode());
                return code;
            }
            protected override void BuildString(ASTStringBuilder builder)
            {
                builder.WriteBlock(nameof(FunctionCall), () =>
                {
                    builder.WriteField(nameof(Name), Name);
                    builder.WriteFieldArray(nameof(Args), Args);
                });
            }
        }

        public class Literal : Expression
        {
            public object Value { get; set; }
            public override object Evaluate() => Value;
            public override bool Equals(object obj)
            {
                return obj is Literal literal &&
                    Equals(literal.Value, Value);
            }
            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }
            protected override void BuildString(ASTStringBuilder builder)
            {
                builder.WriteBlock(nameof(Literal), () =>
                {
                    builder.WriteField(nameof(Value), Value);
                });
            }
        }

        public abstract class UnaryOperator : Expression
        {
            public Expression Operand { get; set; }

            public override object Evaluate()
            {
                var operand = Operand.Evaluate();
                var result = Evaluate(operand);
                if (result == null)
                    GD.PushError($"{GetType().Name}: Could not evaluate with operand of {operand.GetType().Name}.");
                return result;
            }
            public override bool Equals(object obj)
            {
                return obj is UnaryOperator unaryOperator &&
                    obj.GetType() == GetType() &&
                    Equals(Operand, unaryOperator.Operand);
            }
            public override int GetHashCode()
            {
                return Operand.GetHashCode();
            }
            protected abstract object Evaluate(object operand);
            protected override void BuildString(ASTStringBuilder builder)
            {
                string operatorName = GetType().Name.TrimSuffix("Operator");
                builder.WriteBlock(operatorName, () =>
                {
                    builder.WriteField(nameof(Operand), Operand);
                });
            }
        }

        public abstract class PreUnaryOperator : UnaryOperator { }

        public abstract class BinaryOperator : Expression
        {
            public Expression LeftOperand { get; set; }
            public Expression RightOperand { get; set; }

            public override object Evaluate()
            {
                var leftOperand = LeftOperand.Evaluate();
                var rightOperand = RightOperand.Evaluate();
                var result = Evaluate(leftOperand, rightOperand);
                if (result == null)
                    GD.PushError($"{GetType().Name}: Could not evaluate with operands of {leftOperand.GetType().Name} and {rightOperand.GetType().Name}.");
                return result;
            }
            public override bool Equals(object obj)
            {
                return obj is BinaryOperator binaryOperator &&
                    obj.GetType() == GetType() &&
                    Equals(LeftOperand, binaryOperator.LeftOperand) &&
                    Equals(RightOperand, binaryOperator.RightOperand);
            }
            public override int GetHashCode()
            {
                int code = LeftOperand.GetHashCode();
                code = GeneralUtils.CombineHashCodes(code, RightOperand.GetHashCode());
                return code;
            }
            protected abstract object Evaluate(object leftOperand, object rightOperand);
            protected override void BuildString(ASTStringBuilder builder)
            {
                string operatorName = GetType().Name.TrimSuffix("Operator");
                builder.WriteBlock(operatorName, () =>
                {
                    builder.WriteField("Left", LeftOperand);
                    builder.WriteField("Right", RightOperand);
                });
            }
        }

        public class NegativeOperator : PreUnaryOperator
        {
            protected override object Evaluate(object operand)
            {
                if (operand is float)
                    return -(float)operand;
                if (operand is int)
                    return -(int)operand;
                return null;
            }
        }

        public class AddOperator : BinaryOperator
        {
            protected override object Evaluate(object leftOperand, object rightOperand)
            {
                if (leftOperand is float && rightOperand is float)
                    return (float)leftOperand + (float)rightOperand;
                if (leftOperand is float && rightOperand is int)
                    return (float)leftOperand + (int)rightOperand;

                if (leftOperand is int && rightOperand is int)
                    return (int)leftOperand + (int)rightOperand;
                if (leftOperand is int && rightOperand is float)
                    return (int)leftOperand + (float)rightOperand;

                if (leftOperand is string && rightOperand is string)
                    return (string)leftOperand + (string)rightOperand;

                return null;
            }
        }

        public class SubtractOperator : BinaryOperator
        {
            protected override object Evaluate(object leftOperand, object rightOperand)
            {
                if (leftOperand is float && rightOperand is float)
                    return (float)leftOperand - (float)rightOperand;
                if (leftOperand is float && rightOperand is int)
                    return (float)leftOperand - (int)rightOperand;

                if (leftOperand is int && rightOperand is int)
                    return (int)leftOperand - (int)rightOperand;
                if (leftOperand is int && rightOperand is float)
                    return (int)leftOperand - (float)rightOperand;

                return null;
            }
        }

        public class DivideOperator : BinaryOperator
        {
            protected override object Evaluate(object leftOperand, object rightOperand)
            {
                if (leftOperand is float && rightOperand is float)
                    return (float)leftOperand / (float)rightOperand;
                if (leftOperand is float && rightOperand is int)
                    return (float)leftOperand / (int)rightOperand;

                if (leftOperand is int && rightOperand is int)
                    return (int)leftOperand / (int)rightOperand;
                if (leftOperand is int && rightOperand is float)
                    return (int)leftOperand / (float)rightOperand;

                return null;
            }
        }

        public class MultiplyOperator : BinaryOperator
        {
            protected override object Evaluate(object leftOperand, object rightOperand)
            {
                if (leftOperand is float && rightOperand is float)
                    return (float)leftOperand * (float)rightOperand;
                if (leftOperand is float && rightOperand is int)
                    return (float)leftOperand * (int)rightOperand;

                if (leftOperand is int && rightOperand is int)
                    return (int)leftOperand * (int)rightOperand;
                if (leftOperand is int && rightOperand is float)
                    return (int)leftOperand * (float)rightOperand;

                return null;
            }
        }

        public class NegationOperator : PreUnaryOperator
        {
            protected override object Evaluate(object operand)
            {
                if (operand is bool)
                    return !(bool)operand;
                return null;
            }
        }

        public class OrOperator : BinaryOperator
        {
            protected override object Evaluate(object leftOperand, object rightOperand)
            {
                if (leftOperand is bool && rightOperand is bool)
                    return (bool)leftOperand || (bool)rightOperand;
                return null;
            }
        }

        public class AndOperator : BinaryOperator
        {
            protected override object Evaluate(object leftOperand, object rightOperand)
            {
                if (leftOperand is bool && rightOperand is bool)
                    return (bool)leftOperand && (bool)rightOperand;
                return null;
            }
        }

        public class EqualsOperator : BinaryOperator
        {
            protected override object Evaluate(object leftOperand, object rightOperand)
            {
                return leftOperand == rightOperand;
            }
        }

        public class GreaterThanOperator : BinaryOperator
        {
            protected override object Evaluate(object leftOperand, object rightOperand)
            {
                if (leftOperand is float && rightOperand is float)
                    return (float)leftOperand > (float)rightOperand;
                if (leftOperand is float && rightOperand is int)
                    return (float)leftOperand > (int)rightOperand;

                if (leftOperand is int && rightOperand is int)
                    return (int)leftOperand > (int)rightOperand;
                if (leftOperand is int && rightOperand is float)
                    return (int)leftOperand > (float)rightOperand;

                return null;
            }
        }

        public class LessThanOperator : BinaryOperator
        {
            protected override object Evaluate(object leftOperand, object rightOperand)
            {
                if (leftOperand is float && rightOperand is float)
                    return (float)leftOperand < (float)rightOperand;
                if (leftOperand is float && rightOperand is int)
                    return (float)leftOperand < (int)rightOperand;

                if (leftOperand is int && rightOperand is int)
                    return (int)leftOperand < (int)rightOperand;
                if (leftOperand is int && rightOperand is float)
                    return (int)leftOperand < (float)rightOperand;

                return null;
            }
        }

        public class GreaterThanEqualsOperator : GreaterThanOperator
        {
            protected override object Evaluate(object leftOperand, object rightOperand)
            {
                if (leftOperand == rightOperand)
                    return true;
                return base.Evaluate(leftOperand, rightOperand);
            }
        }

        public class LessThanEqualsOperator : LessThanOperator
        {
            protected override object Evaluate(object leftOperand, object rightOperand)
            {
                if (leftOperand == rightOperand)
                    return true;
                return base.Evaluate(leftOperand, rightOperand);
            }
        }
        #endregion

        private int _index;
        private IList<ExpressionLexer.Token> _tokens;
        private Variable.FetchVariableDelegate _fetchVariableFunc;
        private FunctionCall.CallFunctionDelegate _callFunctionFunc;
        private char _eofCharacter = default;

        public int SaveState()
        {
            return _index;
        }

        public void RestoreState(int index)
        {
            _index = index;
        }

        public bool IsEOF()
        {
            return _index >= _tokens.Count;
        }

        public ExpressionLexer.Token NextToken()
        {
            if (_index >= _tokens.Count)
                return null;
            return _tokens[_index++];
        }

        public ExpressionLexer.Token PeekToken(int offset = 0)
        {
            if ((_index + offset) >= _tokens.Count)
                return null;
            return _tokens[_index + offset];
        }

        public string ExpectPuncOrKeyword()
        {
            var nextToken = PeekToken();
            if (nextToken == null)
                return null;
            if (nextToken.TokenType != ExpressionLexer.TokenType.Keyword &&
                nextToken.TokenType != ExpressionLexer.TokenType.Punctuation)
                return null;
            NextToken();
            return (string)nextToken.Value;
        }

        public string ExpectKeyword()
        {
            var nextToken = PeekToken();
            if (nextToken == null)
                return null;
            if (nextToken.TokenType != ExpressionLexer.TokenType.Keyword)
                return null;
            NextToken();
            return (string)nextToken.Value;
        }

        public bool ExpectKeyword(string keyword)
        {
            var nextToken = PeekToken();
            if (nextToken == null)
                return false;
            if (nextToken.TokenType != ExpressionLexer.TokenType.Keyword ||
                !nextToken.Value.Equals(keyword))
                return false;
            NextToken();
            return true;
        }

        public string ExpectPunctuation()
        {
            var nextToken = PeekToken();
            if (nextToken == null)
                return null;
            if (nextToken.TokenType != ExpressionLexer.TokenType.Punctuation)
                return null;
            NextToken();
            return (string)nextToken.Value;
        }

        public bool ExpectPunctuation(string punctuation)
        {
            var nextToken = PeekToken();
            if (nextToken == null)
                return false;
            if (nextToken.TokenType != ExpressionLexer.TokenType.Punctuation ||
                !nextToken.Value.Equals(punctuation))
                return false;
            NextToken();
            return true;
        }

        public string ExpectIdentifier()
        {
            var nextToken = PeekToken();
            if (nextToken == null)
                return null;
            if (nextToken.TokenType != ExpressionLexer.TokenType.Identifier)
                return null;
            NextToken();
            return (string)nextToken.Value;
        }

        public Variable ExpectVariable()
        {
            var identifier = ExpectIdentifier();
            if (identifier == null) return null;
            return new Variable() { Name = identifier, FetchVariable = _fetchVariableFunc };
        }

        public Literal ExpectLiteral()
        {
            var nextToken = PeekToken();
            if (nextToken == null)
                return null;
            if (nextToken.TokenType == ExpressionLexer.TokenType.Number ||
                nextToken.TokenType == ExpressionLexer.TokenType.String)
            {
                NextToken();
                return new Literal() { Value = nextToken.Value };
            }
            if (nextToken.TokenType == ExpressionLexer.TokenType.Keyword)
            {
                if (nextToken.Value.Equals("true"))
                {
                    NextToken();
                    return new Literal() { Value = true };
                }
                if (nextToken.Value.Equals("false"))
                {
                    NextToken();
                    return new Literal() { Value = false };
                }
            }
            return null;
        }

        /// <summary>
        /// Groups operators by precendence. Groups near the start
        /// have higher precedence than groups near the end.
        /// </summary>
        public readonly string[][] Operators = new[]
        {
            new[] { "/", "*", },
            new[] { "+", "-", },
            new[] { ">", "<", ">=", "<=", "==" },
            new[] { "and", "&&", },
            new[] { "or", "||" },
        };

        public string ExpectBinaryOperatorString()
        {
            var state = SaveState();
            var puncOrKeyword = ExpectPuncOrKeyword();
            foreach (var group in Operators)
                if (group.Contains(puncOrKeyword))
                    return puncOrKeyword;
            RestoreState(state);
            return null;
        }

        public int GetOperatorPrecendence(string targetOperator)
        {
            for (int i = 0; i < Operators.Length; i++)
            {
                if (Operators[i].Contains(targetOperator))
                    return Operators.Length - i;
            }
            return -1;
        }

        private BinaryOperator BuildBinaryOperator(string operatorString, Expression leftOperand, Expression rightOperand)
        {
            switch (operatorString)
            {
                case ">": return new GreaterThanOperator() { LeftOperand = leftOperand, RightOperand = rightOperand };
                case "<": return new LessThanOperator() { LeftOperand = leftOperand, RightOperand = rightOperand };
                case ">=": return new GreaterThanEqualsOperator() { LeftOperand = leftOperand, RightOperand = rightOperand };
                case "<=": return new LessThanEqualsOperator() { LeftOperand = leftOperand, RightOperand = rightOperand };
                case "==": return new EqualsOperator() { LeftOperand = leftOperand, RightOperand = rightOperand };
                case "and":
                case "&&": return new AndOperator() { LeftOperand = leftOperand, RightOperand = rightOperand };
                case "or":
                case "||": return new OrOperator() { LeftOperand = leftOperand, RightOperand = rightOperand };
                case "+": return new AddOperator() { LeftOperand = leftOperand, RightOperand = rightOperand };
                case "-": return new SubtractOperator() { LeftOperand = leftOperand, RightOperand = rightOperand };
                case "/": return new DivideOperator() { LeftOperand = leftOperand, RightOperand = rightOperand };
                case "*": return new MultiplyOperator() { LeftOperand = leftOperand, RightOperand = rightOperand };
            }
            return null;
        }

        public BinaryOperator ExpectBinaryOperator(Expression leftOperand = null, string operatorString = null)
        {
            var state = SaveState();
            if (leftOperand == null)
            {
                leftOperand = ExpectNonBinaryExpression();
                if (leftOperand == null) return null;
            }
            if (operatorString == null)
            {
                operatorString = ExpectBinaryOperatorString();
                if (operatorString == null)
                {
                    RestoreState(state);
                    return null;
                }
            }
            var beforeParseRightOperandState = SaveState();
            var rightOperand = ExpectNonBinaryExpression();
            if (rightOperand == null)
            {
                RestoreState(state);
                return null;
            }
            var nextOperatorString = ExpectBinaryOperatorString();
            if (nextOperatorString != null)
            {
                if (GetOperatorPrecendence(nextOperatorString) > GetOperatorPrecendence(operatorString))
                {
                    // We give the RightOperand to the next operator, since it has higher precedence and "steals" it.
                    RestoreState(beforeParseRightOperandState);
                    rightOperand = ExpectBinaryOperator();
                    if (rightOperand == null)
                    {
                        RestoreState(state);
                        return null;
                    }
                    return BuildBinaryOperator(operatorString, leftOperand, rightOperand);
                }
                else
                {
                    // This expression will serve as the LeftOperand of the next BinaryOperator.
                    var builtOperator = BuildBinaryOperator(operatorString, leftOperand, rightOperand);
                    return ExpectBinaryOperator(builtOperator, nextOperatorString);
                }
            }
            else
            {
                return BuildBinaryOperator(operatorString, leftOperand, rightOperand);
            }
            return null;
        }

        public PreUnaryOperator ExpectPreUnaryOperator()
        {
            var state = SaveState();
            var operatorPunc = ExpectPunctuation();
            if (operatorPunc == null) return null;
            var operand = ExpectNonBinaryExpression();
            if (operand == null)
            {
                RestoreState(state);
                return null;
            }
            switch (operatorPunc)
            {
                case "!": return new NegationOperator() { Operand = operand };
                case "-": return new NegativeOperator() { Operand = operand };
            }
            return null;
        }

        public Expression[] ExpectCommaSeparated(int minCount = 0)
        {
            var state = SaveState();
            List<Expression> expressions = new List<Expression>();
            while (true)
            {
                var expression = ExpectExpression();
                if (expression == null)
                {
                    if (expressions.Count == 0)
                        // We're allowed to fail the first expression parsing if
                        // we don't have any expressions at all.
                        break;
                    RestoreState(state);
                    return null;
                }
                expressions.Add(expression);
                if (!ExpectPunctuation(","))
                    break;
            }
            if (expressions.Count < minCount)
                return null;
            return expressions.ToArray();
        }

        public Expression[] ExpectTuple(int minArgs = 0)
        {
            var state = SaveState();
            if (!ExpectPunctuation("(")) return null;
            var expressions = ExpectCommaSeparated(minArgs);
            if (expressions == null || !ExpectPunctuation(")"))
            {
                RestoreState(state);
                return null;
            }
            return expressions;
        }

        public FunctionCall ExpectFunctionCall()
        {
            var state = SaveState();
            var identifier = ExpectIdentifier();
            if (identifier == null) return null;
            var args = ExpectTuple();
            if (args == null)
            {
                RestoreState(state);
                return null;
            }
            return new FunctionCall() { Args = args, Name = identifier, CallFunction = _callFunctionFunc };
        }

        public Expression ExpectParenthesizedExpression()
        {
            var state = SaveState();
            if (!ExpectPunctuation("(")) return null;
            var expression = ExpectExpression();
            if (expression == null || !ExpectPunctuation(")"))
            {
                RestoreState(state);
                return null;
            }
            return expression;
        }

        public Expression ExpectNonBinaryExpression()
        {
            Expression expression = ExpectLiteral();
            if (expression != null) return expression;
            expression = ExpectFunctionCall();
            if (expression != null) return expression;
            expression = ExpectVariable();
            if (expression != null) return expression;
            expression = ExpectParenthesizedExpression();
            if (expression != null) return expression;
            expression = ExpectPreUnaryOperator();
            if (expression != null) return expression;
            return null;
        }

        public Expression ExpectExpression()
        {
            Expression expression = ExpectBinaryOperator();
            if (expression != null) return expression;
            expression = ExpectNonBinaryExpression();
            if (expression != null) return expression;
            return null;
        }

        public Expression Parse(IList<ExpressionLexer.Token> tokens, Variable.FetchVariableDelegate fetchVariableFunc, FunctionCall.CallFunctionDelegate callFunctionFunc)
        {
            _index = 0;
            _tokens = tokens;
            _fetchVariableFunc = fetchVariableFunc;
            _callFunctionFunc = callFunctionFunc;

            return ExpectExpression();
        }
    }
}