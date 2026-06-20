using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NCalc.Domain;

namespace NCalc.Tests
{
    /// <summary>
    /// Characterizes the public Expression API, including cache-observable behavior.
    /// </summary>
    [TestClass]
    public class ExpressionPublicApiTests
    {
        [TestMethod]
        public void Constructor_WithEmptyString_ThrowsArgumentException()
        {
            ArgumentException exception = TestSupport.Throws<ArgumentException>(
                delegate { new Expression(String.Empty); });

            Assert.AreEqual("expression", exception.ParamName);
            TestSupport.StringContains(exception.Message, "Expression can't be empty");
        }

        [TestMethod]
        public void Constructor_WithNullString_ThrowsArgumentException()
        {
            ArgumentException exception = TestSupport.Throws<ArgumentException>(
                delegate { new Expression((string)null); });

            Assert.AreEqual("expression", exception.ParamName);
            TestSupport.StringContains(exception.Message, "Expression can't be empty");
        }

        [TestMethod]
        public void Constructor_WithNullLogicalExpression_ThrowsArgumentException()
        {
            ArgumentException exception = TestSupport.Throws<ArgumentException>(
                delegate { new Expression((LogicalExpression)null); });

            Assert.AreEqual("expression", exception.ParamName);
            TestSupport.StringContains(exception.Message, "Expression can't be null");
        }

        [TestMethod]
        public void Constructor_WithLogicalExpression_UsesProvidedExpression()
        {
            var expression = new Expression(
                new BinaryExpression(
                    BinaryExpressionType.Plus,
                    new ValueExpression(2),
                    new ValueExpression(3)));

            Assert.AreEqual(5, expression.Evaluate());
            Assert.IsNotNull(expression.ParsedExpression);
        }

        [TestMethod]
        public void HasErrors_WithValidExpression_CompilesAndLeavesErrorNull()
        {
            var expression = new Expression("1 + 2");

            Assert.IsFalse(expression.HasErrors());
            Assert.IsNotNull(expression.ParsedExpression);
            Assert.IsNull(expression.Error);
        }

        [TestMethod]
        public void HasErrors_WithInvalidExpression_ReturnsTrueAndStoresError()
        {
            var expression = new Expression("1 +");

            Assert.IsTrue(expression.HasErrors());
            Assert.IsNotNull(expression.Error);
            TestSupport.StringContains(expression.Error, "line");
        }

        [TestMethod]
        public void Evaluate_WithInvalidExpression_ThrowsEvaluationExceptionContainingStoredError()
        {
            var expression = new Expression("(1 + 2");

            EvaluationException exception = TestSupport.Throws<EvaluationException>(
                delegate { expression.Evaluate(); });

            Assert.IsNotNull(expression.Error);
            Assert.AreEqual(expression.Error, exception.Message);
        }

        [TestMethod]
        public void Compile_WithCacheEnabled_CompilesRepeatedExpressionsSuccessfully()
        {
            bool originalCacheEnabled = Expression.CacheEnabled;

            try
            {
                Expression.CacheEnabled = true;
                LogicalExpression first = Expression.Compile("100 + 23", false);
                LogicalExpression second = Expression.Compile("100 + 23", false);

                Assert.IsNotNull(first);
                Assert.IsNotNull(second);
            }
            finally
            {
                Expression.CacheEnabled = originalCacheEnabled;
            }
        }

        [TestMethod]
        public void CacheEnabled_WhenAssigned_RoundTripsAssignedValue()
        {
            bool originalCacheEnabled = Expression.CacheEnabled;

            try
            {
                Expression.CacheEnabled = false;
                Assert.IsFalse(Expression.CacheEnabled);

                Expression.CacheEnabled = true;
                Assert.IsTrue(Expression.CacheEnabled);
            }
            finally
            {
                Expression.CacheEnabled = originalCacheEnabled;
            }
        }

        [TestMethod]
        public void Compile_WithNoCacheArgument_DoesNotReuseCompiledExpression()
        {
            bool originalCacheEnabled = Expression.CacheEnabled;

            try
            {
                Expression.CacheEnabled = true;
                LogicalExpression first = Expression.Compile("200 + 23", true);
                LogicalExpression second = Expression.Compile("200 + 23", true);

                Assert.IsFalse(Object.ReferenceEquals(first, second));
            }
            finally
            {
                Expression.CacheEnabled = originalCacheEnabled;
            }
        }

        [TestMethod]
        public void Compile_WithCacheDisabled_DoesNotReuseCompiledExpression()
        {
            bool originalCacheEnabled = Expression.CacheEnabled;

            try
            {
                Expression.CacheEnabled = false;
                LogicalExpression first = Expression.Compile("300 + 23", false);
                LogicalExpression second = Expression.Compile("300 + 23", false);

                Assert.IsFalse(Object.ReferenceEquals(first, second));
            }
            finally
            {
                Expression.CacheEnabled = originalCacheEnabled;
            }
        }

        [TestMethod]
        public void Evaluate_WithNoCacheOption_EvaluatesSuccessfullyWithoutReusingCompileCache()
        {
            var first = new Expression("400 + 23", EvaluateOptions.NoCache);
            var second = new Expression("400 + 23", EvaluateOptions.NoCache);

            Assert.AreEqual(423, first.Evaluate());
            Assert.AreEqual(423, second.Evaluate());
            Assert.IsFalse(Object.ReferenceEquals(first.ParsedExpression, second.ParsedExpression));
        }
    }
}
