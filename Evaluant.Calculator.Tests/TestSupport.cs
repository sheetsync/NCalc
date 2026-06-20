using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCalc.Tests
{
    /// <summary>
    /// Provides small compatibility assertions for characterization tests.
    /// </summary>
    public static class TestSupport
    {
        public static object Evaluate(string expression)
        {
            return new Expression(expression).Evaluate();
        }

        public static TException Throws<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException exception)
            {
                return exception;
            }

            Assert.Fail("Expected exception of type " + typeof(TException).FullName + ".");
            return null;
        }

        public static void StringContains(string text, string expectedText)
        {
            Assert.IsNotNull(text);
            Assert.IsTrue(
                text.Contains(expectedText),
                "Expected <" + text + "> to contain <" + expectedText + ">.");
        }
    }
}
