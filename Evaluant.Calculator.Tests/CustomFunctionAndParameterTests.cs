using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCalc.Tests
{
    /// <summary>
    /// Characterizes custom callback behavior and event propagation.
    /// </summary>
    [TestClass]
    public class CustomFunctionAndParameterTests
    {
        [TestMethod]
        public void FunctionArgsEvaluateParameters_WithCustomFunction_EvaluatesAllArgumentsInOrder()
        {
            var expression = new Expression("SumAll([a], 2, [b])");
            expression.Parameters["a"] = 3;
            expression.Parameters["b"] = 4;

            expression.EvaluateFunction += delegate(string name, FunctionArgs args)
            {
                if (name == "SumAll")
                {
                    object[] values = args.EvaluateParameters();
                    args.Result = (int)values[0] + (int)values[1] + (int)values[2];
                }
            };

            Assert.AreEqual(9, expression.Evaluate());
        }

        [TestMethod]
        public void CustomFunction_WithNullResult_SetsHasResultAndReturnsNull()
        {
            var expression = new Expression("NullFunction(1)");

            expression.EvaluateFunction += delegate(string name, FunctionArgs args)
            {
                Assert.IsFalse(args.HasResult);
                args.Result = null;
                Assert.IsTrue(args.HasResult);
            };

            Assert.IsNull(expression.Evaluate());
        }

        [TestMethod]
        public void CustomParameter_WithNullResult_SetsHasResultAndReturnsNull()
        {
            var expression = new Expression("[MaybeNull]");

            expression.EvaluateParameter += delegate(string name, ParameterArgs args)
            {
                Assert.IsFalse(args.HasResult);
                args.Result = null;
                Assert.IsTrue(args.HasResult);
            };

            Assert.IsNull(expression.Evaluate());
        }

        [TestMethod]
        public void CustomFunction_WithUnusedArgument_DoesNotEvaluateUnusedArgument()
        {
            var expression = new Expression("First(10, 1 / 0)");

            expression.EvaluateFunction += delegate(string name, FunctionArgs args)
            {
                args.Result = args.Parameters[0].Evaluate();
            };

            Assert.AreEqual(10, expression.Evaluate());
        }

        [TestMethod]
        public void NestedCustomFunctions_WithParameters_PropagateCallbacksToChildExpressions()
        {
            var expression = new Expression("Double(Add([x], 3))");
            expression.Parameters["x"] = 4;

            expression.EvaluateFunction += delegate(string name, FunctionArgs args)
            {
                if (name == "Add")
                {
                    object[] values = args.EvaluateParameters();
                    args.Result = Convert.ToInt32(values[0]) + Convert.ToInt32(values[1]);
                }

                if (name == "Double")
                {
                    args.Result = Convert.ToInt32(args.Parameters[0].Evaluate()) * 2;
                }
            };

            Assert.AreEqual(14, expression.Evaluate());
        }

        [TestMethod]
        public void CustomParameterCallback_WithNoDictionaryValue_ProvidesParameterValue()
        {
            var expression = new Expression("[Dynamic] + 2");

            expression.EvaluateParameter += delegate(string name, ParameterArgs args)
            {
                if (name == "Dynamic")
                {
                    args.Result = 40;
                }
            };

            Assert.AreEqual(42, expression.Evaluate());
        }

        [TestMethod]
        public void CustomParameterCallback_WithNoResult_ThrowsArgumentException()
        {
            var expression = new Expression("[Missing]");

            ArgumentException exception = TestSupport.Throws<ArgumentException>(
                delegate { expression.Evaluate(); });

            TestSupport.StringContains(exception.Message, "Parameter was not defined");
            Assert.AreEqual("Missing", exception.ParamName);
        }

        [TestMethod]
        public void ParameterExpression_WithOuterParameters_UsesOuterParameterValues()
        {
            var expression = new Expression("[total] + [tax]");
            expression.Parameters["total"] = new Expression("[price] * [quantity]");
            expression.Parameters["price"] = 10;
            expression.Parameters["quantity"] = 3;
            expression.Parameters["tax"] = 2;

            Assert.AreEqual(32, expression.Evaluate());
        }
    }
}
