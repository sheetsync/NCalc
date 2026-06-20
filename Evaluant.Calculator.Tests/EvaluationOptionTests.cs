using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCalc.Tests
{
    /// <summary>
    /// Characterizes EvaluateOptions behavior, especially iteration edge cases.
    /// </summary>
    [TestClass]
    public class EvaluationOptionTests
    {
        [TestMethod]
        public void IgnoreCase_WithDifferentFunctionCasing_AllowsBuiltInFunctionLookup()
        {
            Assert.AreEqual(1M, new Expression("aBs(-1)", EvaluateOptions.IgnoreCase).Evaluate());
        }

        [TestMethod]
        public void IgnoreCase_WithCustomFunction_LowersCallbackFunctionName()
        {
            var expression = new Expression("MiXeD(1)", EvaluateOptions.IgnoreCase);
            string callbackName = null;

            expression.EvaluateFunction += delegate(string name, FunctionArgs args)
            {
                callbackName = name;
                args.Result = args.Parameters[0].Evaluate();
            };

            Assert.AreEqual(1, expression.Evaluate());
            Assert.AreEqual("mixed", callbackName);
        }

        [TestMethod]
        public void RoundAwayFromZero_WithMidpointValue_ChangesRoundBehavior()
        {
            Assert.AreEqual(22D, new Expression("Round(22.5, 0)").Evaluate());
            Assert.AreEqual(23D, new Expression("Round(22.5, 0)", EvaluateOptions.RoundAwayFromZero).Evaluate());
        }

        [TestMethod]
        public void IterateParameters_WithMatchingEnumerableParameters_ReturnsResultPerIndex()
        {
            var expression = new Expression("[x] + [y]", EvaluateOptions.IterateParameters);
            expression.Parameters["x"] = new[] { 1, 2, 3 };
            expression.Parameters["y"] = new[] { 10, 20, 30 };

            var result = (IList)expression.Evaluate();

            Assert.HasCount(3, result);
            Assert.AreEqual(11, result[0]);
            Assert.AreEqual(22, result[1]);
            Assert.AreEqual(33, result[2]);
        }

        [TestMethod]
        public void IterateParameters_WithMismatchedEnumerableLengths_ThrowsEvaluationException()
        {
            var expression = new Expression("[x] + [y]", EvaluateOptions.IterateParameters);
            expression.Parameters["x"] = new[] { 1, 2 };
            expression.Parameters["y"] = new[] { 10, 20, 30 };

            EvaluationException exception = TestSupport.Throws<EvaluationException>(
                delegate { expression.Evaluate(); });

            TestSupport.StringContains(exception.Message, "IEnumerable parameters must have the same number of items");
        }

        [TestMethod]
        public void IterateParameters_WithMixedScalarAndEnumerableParameters_ReusesScalarForEveryIndex()
        {
            var expression = new Expression("[x] + [offset]", EvaluateOptions.IterateParameters);
            expression.Parameters["x"] = new[] { 1, 2, 3 };
            expression.Parameters["offset"] = 10;

            var result = (IList)expression.Evaluate();

            Assert.HasCount(3, result);
            Assert.AreEqual(11, result[0]);
            Assert.AreEqual(12, result[1]);
            Assert.AreEqual(13, result[2]);
        }

        [TestMethod]
        public void IterateParameters_WithEmptyEnumerable_ReturnsEmptyResultList()
        {
            var expression = new Expression("[x] + 1", EvaluateOptions.IterateParameters);
            expression.Parameters["x"] = new int[0];

            var result = (IList)expression.Evaluate();

            Assert.IsEmpty(result);
        }

        [TestMethod]
        public void IterateParameters_WithOnlyScalarParameters_ReturnsEmptyResultList()
        {
            var expression = new Expression("[x] + 1", EvaluateOptions.IterateParameters);
            expression.Parameters["x"] = 1;

            var result = (IList)expression.Evaluate();

            Assert.IsEmpty(result);
        }

        [TestMethod]
        public void IterateParameters_AfterSuccessfulEvaluation_LeavesEnumerableParameterAtLastItem()
        {
            var expression = new Expression("[x] + [offset]", EvaluateOptions.IterateParameters);
            expression.Parameters["x"] = new[] { 1, 2, 3 };
            expression.Parameters["offset"] = 10;

            expression.Evaluate();

            Assert.AreEqual(3, expression.Parameters["x"]);
            Assert.AreEqual(10, expression.Parameters["offset"]);
        }
    }
}
