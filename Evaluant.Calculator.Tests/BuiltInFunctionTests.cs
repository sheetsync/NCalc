using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCalc.Tests
{
    /// <summary>
    /// Characterizes built-in functions, including current arity validation.
    /// </summary>
    [TestClass]
    public class BuiltInFunctionTests
    {
        [TestMethod]
        public void MathFunctions_WithRepresentativeInputs_ReturnExpectedResults()
        {
            object[,] cases =
            {
                { "Abs(-12)", 12M },
                { "Ceiling(1.1)", 2D },
                { "Floor(1.9)", 1D },
                { "Max(10, 20)", 20 },
                { "Min(10, 20)", 10 },
                { "Max(1.5, 2.5)", 2.5D },
                { "Min(1.5, 2.5)", 1.5D },
                { "Pow(2, 8)", 256D },
                { "Round(3.456, 2)", 3.46D },
                { "Sign(-12)", -1 },
                { "Truncate(3.9)", 3D }
            };

            for (int i = 0; i < cases.GetLength(0); i++)
            {
                Assert.AreEqual(cases[i, 1], TestSupport.Evaluate((string)cases[i, 0]), (string)cases[i, 0]);
            }
        }

        [TestMethod]
        public void TrigonometricFunctions_WithRepresentativeInputs_ReturnExpectedResults()
        {
            object[,] cases =
            {
                { "Acos(1)", 0D },
                { "Asin(0)", 0D },
                { "Atan(0)", 0D },
                { "Cos(0)", 1D },
                { "Exp(0)", 1D },
                { "IEEERemainder(3, 2)", -1D },
                { "Log(1, 10)", 0D },
                { "Log10(1)", 0D },
                { "Sin(0)", 0D },
                { "Sqrt(9)", 3D },
                { "Tan(0)", 0D }
            };

            for (int i = 0; i < cases.GetLength(0); i++)
            {
                Assert.AreEqual(cases[i, 1], TestSupport.Evaluate((string)cases[i, 0]), (string)cases[i, 0]);
            }
        }

        [TestMethod]
        public void BuiltInFunctions_WithWrongArity_ThrowArgumentException()
        {
            string[,] cases =
            {
                { "Abs()", "Abs() takes exactly 1 argument" },
                { "Acos()", "Acos() takes exactly 1 argument" },
                { "Asin()", "Asin() takes exactly 1 argument" },
                { "Atan()", "Atan() takes exactly 1 argument" },
                { "Ceiling()", "Ceiling() takes exactly 1 argument" },
                { "Cos()", "Cos() takes exactly 1 argument" },
                { "Exp()", "Exp() takes exactly 1 argument" },
                { "Floor()", "Floor() takes exactly 1 argument" },
                { "IEEERemainder(1)", "IEEERemainder() takes exactly 2 arguments" },
                { "Log(1)", "Log() takes exactly 2 arguments" },
                { "Log10()", "Log10() takes exactly 1 argument" },
                { "Pow(1)", "Pow() takes exactly 2 arguments" },
                { "Round(1)", "Round() takes exactly 2 arguments" },
                { "Sign()", "Sign() takes exactly 1 argument" },
                { "Sin()", "Sin() takes exactly 1 argument" },
                { "Sqrt()", "Sqrt() takes exactly 1 argument" },
                { "Tan()", "Tan() takes exactly 1 argument" },
                { "Truncate()", "Truncate() takes exactly 1 argument" },
                { "Max(1)", "Max() takes exactly 2 arguments" },
                { "Min(1)", "Min() takes exactly 2 arguments" },
                { "if(true, 1)", "if() takes exactly 3 arguments" },
                { "in(1)", "in() takes at least 2 arguments" }
            };

            for (int i = 0; i < cases.GetLength(0); i++)
            {
                ArgumentException exception = TestSupport.Throws<ArgumentException>(
                    delegate { new Expression(cases[i, 0]).Evaluate(); });

                TestSupport.StringContains(exception.Message, cases[i, 1]);
            }
        }

        [TestMethod]
        public void BuiltInFunctions_WithWrongCaseWhenCaseSensitive_ThrowHelpfulArgumentException()
        {
            ArgumentException exception = TestSupport.Throws<ArgumentException>(
                delegate { new Expression("max(1, 2)").Evaluate(); });

            TestSupport.StringContains(exception.Message, "Function not found max. Try Max instead.");
        }

        [TestMethod]
        public void UnknownFunction_WithNoCustomHandler_ThrowsArgumentException()
        {
            ArgumentException exception = TestSupport.Throws<ArgumentException>(
                delegate { new Expression("DoesNotExist(1)").Evaluate(); });

            TestSupport.StringContains(exception.Message, "Function not found");
            Assert.AreEqual("DoesNotExist", exception.ParamName);
        }

        [TestMethod]
        public void IfFunction_WithUnusedBranchContainingError_DoesNotEvaluateUnusedBranch()
        {
            Assert.AreEqual(42, TestSupport.Evaluate("if(true, 42, 1 / 0)"));
            Assert.AreEqual(42, TestSupport.Evaluate("if(false, 1 / 0, 42)"));
        }

        [TestMethod]
        public void InFunction_WithMatchBeforeInvalidExpression_DoesNotEvaluateRemainingArguments()
        {
            Assert.AreEqual(true, TestSupport.Evaluate("in(4, 1, 2, 4, 1 / 0)"));
        }
    }
}
