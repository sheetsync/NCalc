using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCalc.Tests
{
    [TestClass]
    public class ExpressionNumericBehaviorTests
    {
        [TestMethod]
        public void ExpressionEvaluate_WithNumericLiterals_ReturnsCharacterizedRuntimeTypes()
        {
            VerifyAll(
                new ExpressionResultCase("integer literal", "5", 5, typeof(int)),
                new ExpressionResultCase("large integer literal overflow path", "40000000000", 40000000000f, typeof(float)),
                new ExpressionResultCase("floating literal", "1.5", 1.5d, typeof(double)),
                new ExpressionResultCase("scientific notation", "1.22e1", 12.2d, typeof(double)));
        }

        [TestMethod]
        public void ExpressionEvaluate_WithNumericArithmetic_ReturnsCharacterizedRuntimeTypes()
        {
            VerifyAll(
                new ExpressionResultCase("integer division is promoted by evaluation visitor", "3/6", 0.5d, typeof(double)),
                new ExpressionResultCase("integer modulo remains int", "10 % 4", 2, typeof(int)),
                new ExpressionResultCase("unary negation remains int", "-5", -5, typeof(int)),
                new ExpressionResultCase("large integer addition uses single", "40000000000+1", 40000000000 + 1f, typeof(float)),
                new ExpressionResultCase("string numeric addition uses decimal conversion", "1 + '2'", 3m, typeof(decimal)));
        }

        [TestMethod]
        public void ExpressionEvaluate_WithDecimalDoubleMixing_ReturnsCharacterizedRuntimeTypes()
        {
            Expression expression = new Expression("1.8 + Abs([var1])");
            expression.Parameters["var1"] = 9.2;

            object actualValue = expression.Evaluate();

            Assert.AreEqual(typeof(decimal), actualValue.GetType());
            Assert.AreEqual(11.0m, actualValue);
        }

        [TestMethod]
        public void ExpressionEvaluate_WithParameterizedRealTypes_PreservesCurrentRealResultTypes()
        {
            Expression floatExpression = new Expression("x/2");
            floatExpression.Parameters["x"] = 2f;

            Expression doubleExpression = new Expression("x/2");
            doubleExpression.Parameters["x"] = 2d;

            Expression decimalExpression = new Expression("x/2");
            decimalExpression.Parameters["x"] = 2m;

            Assert.AreEqual(typeof(float), floatExpression.Evaluate().GetType());
            Assert.AreEqual(typeof(double), doubleExpression.Evaluate().GetType());
            Assert.AreEqual(typeof(decimal), decimalExpression.Evaluate().GetType());
        }

        private static void VerifyAll(params ExpressionResultCase[] cases)
        {
            foreach (ExpressionResultCase testCase in cases)
            {
                testCase.Verify();
            }
        }
    }
}
