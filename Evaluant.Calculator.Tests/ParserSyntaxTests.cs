using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCalc.Tests
{
    /// <summary>
    /// Characterizes parser grammar and operator behavior at the public API boundary.
    /// </summary>
    [TestClass]
    public class ParserSyntaxTests
    {
        [TestMethod]
        public void Literals_WithSupportedSyntax_ParseToExpectedValues()
        {
            object[,] cases =
            {
                { "123", 123 },
                { "40000000000", 40000000000F },
                { "123.456", 123.456D },
                { "1.22e1", 12.2D },
                { "1e-2", 0.01D },
                { "true", true },
                { "false", false },
                { "'text'", "text" },
                { @"'\u0048\u0065\u006C\u006C\u006F'", "Hello" },
                { @"'line\nbreak'", "line\nbreak" },
                { "#01/01/2001#", new DateTime(2001, 1, 1) }
            };

            for (int i = 0; i < cases.GetLength(0); i++)
            {
                Assert.AreEqual(cases[i, 1], TestSupport.Evaluate((string)cases[i, 0]), (string)cases[i, 0]);
            }
        }

        [TestMethod]
        public void Identifiers_WithPlainAndBracketedNames_ReadParameterValues()
        {
            var expression = new Expression("plain + [customer age] + [_under123]");
            expression.Parameters["plain"] = 1;
            expression.Parameters["customer age"] = 2;
            expression.Parameters["_under123"] = 3;

            Assert.AreEqual(6, expression.Evaluate());
        }

        [TestMethod]
        public void Parser_WithMalformedInputs_ReportsSyntaxErrorsBeforeEvaluation()
        {
            string[] cases =
            {
                "(",
                "()",
                "1 +",
                "'unterminated",
                "if(true,,1)"
            };

            for (int i = 0; i < cases.Length; i++)
            {
                var expression = new Expression(cases[i]);
                Assert.IsTrue(expression.HasErrors(), cases[i]);
                Assert.IsNotNull(expression.Error, cases[i]);
            }
        }

        [TestMethod]
        public void Operators_WithPrecedenceAndAssociativity_EvaluateCurrentGrammarBehavior()
        {
            object[,] cases =
            {
                { "2 + 3 * 4", 14 },
                { "(2 + 3) * 4", 20 },
                { "18 / 2 / 2 * 3", 13.5D },
                { "1 + 2 + 3 * 4 / 2", 9D },
                { "2 << 1 + 1", 8 },
                { "8 >> 1 + 1", 2 },
                { "1 | 2 & 3", 3 },
                { "1 ^ 3 & 1", 0 },
                { "~1", -2 },
                { "-1 + 2", 1 },
                { "not false and true", true },
                { "false || true && false", false }
            };

            for (int i = 0; i < cases.GetLength(0); i++)
            {
                Assert.AreEqual(cases[i, 1], TestSupport.Evaluate((string)cases[i, 0]), (string)cases[i, 0]);
            }
        }

        [TestMethod]
        public void TernaryExpression_WithNestedParenthesizedTernary_EvaluatesExpectedBranch()
        {
            Assert.AreEqual(2, TestSupport.Evaluate("false ? 1 : (true ? 2 : 3)"));
            Assert.AreEqual(1, TestSupport.Evaluate("true ? 1 : (true ? 2 : 3)"));
        }

        [TestMethod]
        public void ComparisonOperators_WithMixedNumericValues_EvaluateExpectedBooleans()
        {
            object[,] cases =
            {
                { "1 = 1.0", true },
                { "1 == 1.0", true },
                { "1 != 1.0", false },
                { "1 <> 2", true },
                { "2 > 1", true },
                { "2 >= 2", true },
                { "1 < 2", true },
                { "1 <= 1", true }
            };

            for (int i = 0; i < cases.GetLength(0); i++)
            {
                Assert.AreEqual(cases[i, 1], TestSupport.Evaluate((string)cases[i, 0]), (string)cases[i, 0]);
            }
        }
    }
}
