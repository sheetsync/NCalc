using System;
using System.Globalization;
using System.Threading;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using NCalc.Domain;
using DomainValueType = NCalc.Domain.ValueType;

namespace NCalc.FrameworkCompatibility.Tests
{
    internal delegate object BinaryOperation(object left, object right);

    internal static class Program
    {
        private static int _passed;
        private static int _failed;

        private static int Main()
        {
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            CultureInfo originalUserInterfaceCulture = Thread.CurrentThread.CurrentUICulture;

            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr-FR");

                RunExpressionCases();
                RunParserSurfaceCases();
                RunNumbersCases();
                RunDomainCases();
                RunCallbackCases();
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalUserInterfaceCulture;
            }

            if (_failed > 0)
            {
                Console.Error.WriteLine("NCalc framework compatibility failed: " + _failed + " failed, " + _passed + " passed.");
                return 1;
            }

            Console.WriteLine("NCalc framework compatibility passed: " + _passed + " assertions.");
            return 0;
        }

        private static void RunExpressionCases()
        {
            Run("ExpressionEvaluate_WithInvariantFloatLiteralUnderFrenchCulture_DoesReturnDouble", delegate
            {
                AssertExpression("1.5 + 2.25", 3.75d, typeof(double));
            });

            Run("ExpressionEvaluate_WithLargeIntegerLiteral_DoesPreserveSingleOverflowPath", delegate
            {
                AssertExpression("40000000000", 40000000000f, typeof(float));
            });

            Run("ExpressionEvaluate_WithDateLiteralUnderFrenchCulture_DoesReturnDateTime", delegate
            {
                AssertExpression("#01/01/2001#", new DateTime(2001, 1, 1), typeof(DateTime));
            });

            Run("ExpressionEvaluate_WithUnicodeAndNewlineEscapes_DoesUnescapeString", delegate
            {
                AssertExpression("'\\u0041\\n'", "A\n", typeof(string));
            });

            Run("ExpressionEvaluate_WithIgnoreCaseBuiltInFunction_DoesResolveFunctionName", delegate
            {
                AssertObject(1m, typeof(decimal), new Expression("aBs(-1)", EvaluateOptions.IgnoreCase).Evaluate());
            });

            Run("ExpressionEvaluate_WithDecimalDoubleMix_DoesReturnDecimal", delegate
            {
                Expression expression = new Expression("1.8 + Abs([var1])");
                expression.Parameters["var1"] = 9.2d;

                AssertObject(11.0m, typeof(decimal), expression.Evaluate());
            });
        }

        private static void RunParserSurfaceCases()
        {
            Run("ParserNcalcExpression_WithLegacyReturnScopeVariable_DoesExposeAstTree", delegate
            {
                NCalcLexer lexer = new NCalcLexer(new ANTLRStringStream("1 + 2"));
                CommonTokenStream tokens = new CommonTokenStream(lexer);
                NCalcParser parser = new NCalcParser(tokens);

                ParserRuleReturnScope parserResult = parser.ncalcExpression();
                IAstRuleReturnScope astResult = (IAstRuleReturnScope)parserResult;
                CommonTree tree = astResult.Tree as CommonTree;

                if (tree == null)
                {
                    throw new InvalidOperationException("Expected legacy parser return scope to expose a CommonTree.");
                }

                if (parserResult.Start == null || parserResult.Stop == null)
                {
                    throw new InvalidOperationException("Expected legacy parser return scope to expose token boundaries.");
                }
            });
        }

        private static void RunNumbersCases()
        {
            Run("NumbersAdd_WithStringCommaDecimalUnderFrenchCulture_DoesParseUsingCurrentCulture", delegate
            {
                AssertOperation(Numbers.Add, "4,5", 1, 5.5m, typeof(decimal));
            });

            Run("NumbersAdd_WithDoubleAndDecimal_DoesReturnDecimal", delegate
            {
                AssertOperation(Numbers.Add, 2.5d, 3.5m, 6.0m, typeof(decimal));
            });

            Run("NumbersSoustract_WithByteAndByte_DoesPreserveNullFallthrough", delegate
            {
                AssertNull(Numbers.Soustract((byte)2, (byte)2));
            });

            Run("NumbersSoustract_WithUnsignedOperands_DoesPreserveUncheckedWraparound", delegate
            {
                AssertOperation(Numbers.Soustract, (uint)6, (ulong)8, UInt64.MaxValue - 1, typeof(ulong));
            });

            Run("NumbersDivide_WithUlongAndBoolean_DoesPreserveWrongOperatorMessage", delegate
            {
                AssertThrows(
                    delegate { Numbers.Divide((ulong)8, true); },
                    typeof(InvalidOperationException),
                    "Operator '-'");
            });

            Run("NumbersModulo_WithDecimalAndDouble_DoesPreserveDecimalMessageTypo", delegate
            {
                AssertThrows(
                    delegate { Numbers.Modulo(3.5m, 2.5d); },
                    typeof(InvalidOperationException),
                    "decimal' and 'decimal");
            });

            Run("NumbersMax_WithByteAndDecimal_DoesUseLeftOperandConversion", delegate
            {
                AssertOperation(Numbers.Max, (byte)2, 3.5m, (byte)4, typeof(byte));
            });

            Run("NumbersMin_WithUshortAndDouble_DoesUseLeftOperandConversion", delegate
            {
                AssertOperation(Numbers.Min, (ushort)4, 2.5d, (ushort)2, typeof(ushort));
            });
        }

        private static void RunDomainCases()
        {
            Run("ValueExpression_WithFloatUnderFrenchCulture_DoesSerializeWithInvariantDecimalSeparator", delegate
            {
                AssertEqual("1.5", new ValueExpression(1.5f).ToString());
            });

            Run("ValueExpression_WithUnsupportedObject_DoesThrowEvaluationException", delegate
            {
                AssertThrows(
                    delegate { new ValueExpression(new object()); },
                    typeof(EvaluationException),
                    "This value could not be handled");
            });

            Run("ExpressionEvaluate_WithManualAst_DoesUseSameArithmeticPipeline", delegate
            {
                Expression expression = new Expression(
                    new BinaryExpression(
                        BinaryExpressionType.Plus,
                        new ValueExpression(2),
                        new ValueExpression(3)));

                AssertObject(5, typeof(int), expression.Evaluate());
            });

            Run("ValueExpression_WithDateTime_DoesInferDateTimeType", delegate
            {
                DateTime dateTime = new DateTime(2026, 6, 20);
                ValueExpression expression = new ValueExpression(dateTime);

                AssertEqual(DomainValueType.DateTime, expression.Type);
                AssertEqual(dateTime, expression.Value);
            });
        }

        private static void RunCallbackCases()
        {
            Run("ExpressionEvaluate_WithLazyTernary_DoesNotResolveUnusedBranch", delegate
            {
                Expression expression = new Expression("if([flag], [value], [missing])");
                expression.Parameters["flag"] = true;
                expression.Parameters["value"] = 42;
                expression.EvaluateParameter += delegate(string name, ParameterArgs args)
                {
                    if (name == "missing")
                    {
                        throw new InvalidOperationException("Unused branch was evaluated.");
                    }
                };

                AssertObject(42, typeof(int), expression.Evaluate());
            });

            Run("ExpressionEvaluate_WithCustomFunction_DoesPreserveLazyParameterEvaluation", delegate
            {
                Expression expression = new Expression("twice([value] + 1)");
                expression.Parameters["value"] = 20;
                expression.EvaluateFunction += delegate(string name, FunctionArgs args)
                {
                    if (name == "twice")
                    {
                        args.Result = Convert.ToInt32(args.Parameters[0].Evaluate()) * 2;
                    }
                };

                AssertObject(42, typeof(int), expression.Evaluate());
            });
        }

        private static void AssertExpression(string expression, object expectedValue, Type expectedType)
        {
            AssertObject(expectedValue, expectedType, new Expression(expression).Evaluate());
        }

        private static void AssertOperation(BinaryOperation operation, object left, object right, object expectedValue, Type expectedType)
        {
            AssertObject(expectedValue, expectedType, operation(left, right));
        }

        private static void AssertObject(object expectedValue, Type expectedType, object actualValue)
        {
            if (actualValue == null)
            {
                throw new InvalidOperationException("Expected value <" + expectedValue + "> but got null.");
            }

            if (actualValue.GetType() != expectedType)
            {
                throw new InvalidOperationException("Expected type <" + expectedType.FullName + "> but got <" + actualValue.GetType().FullName + ">.");
            }

            AssertEqual(expectedValue, actualValue);
        }

        private static void AssertNull(object actualValue)
        {
            if (actualValue != null)
            {
                throw new InvalidOperationException("Expected null but got <" + actualValue + ">.");
            }
        }

        private static void AssertEqual(object expectedValue, object actualValue)
        {
            if (!object.Equals(expectedValue, actualValue))
            {
                throw new InvalidOperationException("Expected <" + expectedValue + "> but got <" + actualValue + ">.");
            }
        }

        private static void AssertThrows(Action action, Type expectedExceptionType, string expectedMessageFragment)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                if (exception.GetType() != expectedExceptionType)
                {
                    throw new InvalidOperationException("Expected exception <" + expectedExceptionType.FullName + "> but got <" + exception.GetType().FullName + ">.", exception);
                }

                if (exception.Message.IndexOf(expectedMessageFragment, StringComparison.Ordinal) < 0)
                {
                    throw new InvalidOperationException("Expected message <" + exception.Message + "> to contain <" + expectedMessageFragment + ">.");
                }

                return;
            }

            throw new InvalidOperationException("Expected exception <" + expectedExceptionType.FullName + ">.");
        }

        private static void Run(string name, Action action)
        {
            try
            {
                action();
                _passed++;
                Console.WriteLine("PASS " + name);
            }
            catch (Exception exception)
            {
                _failed++;
                Console.Error.WriteLine("FAIL " + name);
                Console.Error.WriteLine(exception);
            }
        }
    }
}
