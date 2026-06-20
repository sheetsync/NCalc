using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCalc.Tests
{
    /// <summary>
    /// Characterizes the public EvaluationException constructors.
    /// </summary>
    [TestClass]
    public class EvaluationExceptionTests
    {
        [TestMethod]
        public void Constructor_WithMessage_SetsMessageAndLeavesInnerExceptionNull()
        {
            var exception = new EvaluationException("evaluation failed");

            Assert.IsInstanceOfType(exception, typeof(ApplicationException));
            Assert.AreEqual("evaluation failed", exception.Message);
            Assert.IsNull(exception.InnerException);
        }

        [TestMethod]
        public void Constructor_WithMessageAndInnerException_SetsMessageAndInnerException()
        {
            var innerException = new InvalidOperationException("inner failure");

            var exception = new EvaluationException("evaluation failed", innerException);

            Assert.IsInstanceOfType(exception, typeof(ApplicationException));
            Assert.AreEqual("evaluation failed", exception.Message);
            Assert.AreSame(innerException, exception.InnerException);
        }
    }
}
