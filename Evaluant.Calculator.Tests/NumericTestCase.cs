using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCalc.Tests
{
    internal delegate object NumericOperation(object left, object right);

    internal sealed class NumericResultCase
    {
        public NumericResultCase(string name, NumericOperation operation, object left, object right, object expectedValue, Type expectedType)
        {
            Name = name;
            Operation = operation;
            Left = left;
            Right = right;
            ExpectedValue = expectedValue;
            ExpectedType = expectedType;
        }

        private string Name { get; set; }
        private NumericOperation Operation { get; set; }
        private object Left { get; set; }
        private object Right { get; set; }
        private object ExpectedValue { get; set; }
        private Type ExpectedType { get; set; }

        public void Verify()
        {
            object actualValue = Operation(Left, Right);

            if (ExpectedType == null)
            {
                Assert.IsNull(actualValue, Name + " should return null");
                return;
            }

            Assert.IsNotNull(actualValue, Name + " should not return null");
            Assert.AreEqual(ExpectedType, actualValue.GetType(), Name + " result type");
            Assert.AreEqual(ExpectedValue, actualValue, Name + " result value");
        }
    }

    internal sealed class NumericExceptionCase
    {
        public NumericExceptionCase(string name, NumericOperation operation, object left, object right, Type expectedExceptionType, string expectedMessageFragment)
        {
            Name = name;
            Operation = operation;
            Left = left;
            Right = right;
            ExpectedExceptionType = expectedExceptionType;
            ExpectedMessageFragment = expectedMessageFragment;
        }

        private string Name { get; set; }
        private NumericOperation Operation { get; set; }
        private object Left { get; set; }
        private object Right { get; set; }
        private Type ExpectedExceptionType { get; set; }
        private string ExpectedMessageFragment { get; set; }

        public void Verify()
        {
            try
            {
                Operation(Left, Right);
                Assert.Fail(Name + " should throw " + ExpectedExceptionType.Name);
            }
            catch (Exception exception)
            {
                Assert.AreEqual(ExpectedExceptionType, exception.GetType(), Name + " exception type");

                if (!String.IsNullOrEmpty(ExpectedMessageFragment))
                {
                    Assert.IsTrue(
                        exception.Message.IndexOf(ExpectedMessageFragment) >= 0,
                        Name + " message should contain '" + ExpectedMessageFragment + "' but was '" + exception.Message + "'");
                }
            }
        }
    }

    internal sealed class ExpressionResultCase
    {
        public ExpressionResultCase(string name, string expression, object expectedValue, Type expectedType)
        {
            Name = name;
            Expression = expression;
            ExpectedValue = expectedValue;
            ExpectedType = expectedType;
        }

        private string Name { get; set; }
        private string Expression { get; set; }
        private object ExpectedValue { get; set; }
        private Type ExpectedType { get; set; }

        public void Verify()
        {
            object actualValue = new Expression(Expression).Evaluate();

            Assert.IsNotNull(actualValue, Name + " should not return null");
            Assert.AreEqual(ExpectedType, actualValue.GetType(), Name + " result type");
            Assert.AreEqual(ExpectedValue, actualValue, Name + " result value");
        }
    }
}
