using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NCalc.Domain;

namespace NCalc.Tests
{
    /// <summary>
    /// Characterizes domain expression serialization through ToString().
    /// </summary>
    [TestClass]
    public class SerializationTests
    {
        [TestMethod]
        public void ToString_WithBinaryOperators_SerializesCurrentOperatorTokens()
        {
            object[,] cases =
            {
                { BinaryExpressionType.And, "True and False" },
                { BinaryExpressionType.Or, "True or False" },
                { BinaryExpressionType.Equal, "1 = 2" },
                { BinaryExpressionType.NotEqual, "1 != 2" },
                { BinaryExpressionType.Lesser, "1 < 2" },
                { BinaryExpressionType.LesserOrEqual, "1 <= 2" },
                { BinaryExpressionType.Greater, "1 > 2" },
                { BinaryExpressionType.GreaterOrEqual, "1 >= 2" },
                { BinaryExpressionType.Plus, "1 + 2" },
                { BinaryExpressionType.Minus, "1 - 2" },
                { BinaryExpressionType.Times, "1 * 2" },
                { BinaryExpressionType.Div, "1 / 2" },
                { BinaryExpressionType.Modulo, "1 % 2" },
                { BinaryExpressionType.BitwiseAnd, "1 & 2" },
                { BinaryExpressionType.BitwiseOr, "1 | 2" },
                { BinaryExpressionType.BitwiseXOr, "1 ~ 2" },
                { BinaryExpressionType.LeftShift, "1 << 2" },
                { BinaryExpressionType.RightShift, "1 >> 2" }
            };

            for (int i = 0; i < cases.GetLength(0); i++)
            {
                LogicalExpression left = IsBooleanOperator((BinaryExpressionType)cases[i, 0])
                    ? (LogicalExpression)new ValueExpression(true)
                    : new ValueExpression(1);
                LogicalExpression right = IsBooleanOperator((BinaryExpressionType)cases[i, 0])
                    ? (LogicalExpression)new ValueExpression(false)
                    : new ValueExpression(2);

                Assert.AreEqual(
                    cases[i, 1],
                    new BinaryExpression((BinaryExpressionType)cases[i, 0], left, right).ToString());
            }
        }

        [TestMethod]
        public void ToString_WithUnaryOperators_SerializesCurrentOperatorTokens()
        {
            Assert.AreEqual("!True", new UnaryExpression(UnaryExpressionType.Not, new ValueExpression(true)).ToString());
            Assert.AreEqual("-1", new UnaryExpression(UnaryExpressionType.Negate, new ValueExpression(1)).ToString());
            Assert.AreEqual("~1", new UnaryExpression(UnaryExpressionType.BitwiseNot, new ValueExpression(1)).ToString());
        }

        [TestMethod]
        public void ToString_WithTernaryExpression_SerializesBranches()
        {
            var expression = new TernaryExpression(
                new ValueExpression(true),
                new ValueExpression(1),
                new ValueExpression(0));

            Assert.AreEqual("True ? 1 : 0", expression.ToString());
        }

        [TestMethod]
        public void ToString_WithFunctionIdentifierAndValues_SerializesFunctionCall()
        {
            var expression = new Function(
                new Identifier("Mix"),
                new LogicalExpression[]
                {
                    new Identifier("name"),
                    new BinaryExpression(BinaryExpressionType.Plus, new ValueExpression(1), new ValueExpression(2)),
                    new ValueExpression(new DateTime(2009, 1, 1))
                });

            Assert.AreEqual("Mix([name], 1 + 2, #" + new DateTime(2009, 1, 1) + "#)", expression.ToString());
        }

        [TestMethod]
        public void FluentDomainMethods_WithValueOperands_CreateSerializableBinaryExpressions()
        {
            Assert.AreEqual("1 + 2", new ValueExpression(1).Plus(2).ToString());
            Assert.AreEqual("1 - 2", new ValueExpression(1).Minus(2).ToString());
            Assert.AreEqual("1 * 2", new ValueExpression(1).Mult(2).ToString());
            Assert.AreEqual("1 / 2", new ValueExpression(1).DividedBy(2).ToString());
            Assert.AreEqual("1 << 2", new ValueExpression(1).LeftShift(2).ToString());
            Assert.AreEqual("1 >> 2", new ValueExpression(1).RightShift(2).ToString());
        }

        private static bool IsBooleanOperator(BinaryExpressionType type)
        {
            return type == BinaryExpressionType.And || type == BinaryExpressionType.Or;
        }
    }
}
