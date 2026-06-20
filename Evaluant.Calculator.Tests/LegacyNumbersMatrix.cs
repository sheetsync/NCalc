using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCalc.Tests
{
    // Defines the legacy numeric dispatch contract exercised by the matrix tests.
    internal enum LegacyNumbersOperator
    {
        Add,
        Soustract,
        Multiply,
        Divide,
        Modulo,
        Max,
        Min
    }

    internal sealed class LegacyNumericOperand
    {
        public LegacyNumericOperand(string name, object value, TypeCode typeCode, string legacyTypeName)
        {
            Name = name;
            Value = value;
            TypeCode = typeCode;
            LegacyTypeName = legacyTypeName;
        }

        public string Name { get; }
        public object Value { get; }
        public TypeCode TypeCode { get; }
        public string LegacyTypeName { get; }
    }

    internal sealed class LegacyNumericOutcome
    {
        private LegacyNumericOutcome(object expectedValue, Type expectedExceptionType, string expectedExceptionMessage, bool returnsNull)
        {
            ExpectedValue = expectedValue;
            ExpectedExceptionType = expectedExceptionType;
            ExpectedExceptionMessage = expectedExceptionMessage;
            ReturnsNull = returnsNull;
        }

        public object ExpectedValue { get; }
        public Type ExpectedExceptionType { get; }
        public string ExpectedExceptionMessage { get; }
        public bool ReturnsNull { get; }

        public bool ThrowsException => ExpectedExceptionType != null;

        public static LegacyNumericOutcome Value(object expectedValue)
        {
            return new LegacyNumericOutcome(expectedValue, null, null, false);
        }

        public static LegacyNumericOutcome Null()
        {
            return new LegacyNumericOutcome(null, null, null, true);
        }

        public static LegacyNumericOutcome Exception(Type expectedExceptionType, string expectedExceptionMessage)
        {
            return new LegacyNumericOutcome(null, expectedExceptionType, expectedExceptionMessage, false);
        }
    }

    internal static class LegacyNumbersMatrix
    {
        private static readonly LegacyNumericOperand[] PrimitiveOperands =
        {
            new LegacyNumericOperand("bool true", true, TypeCode.Boolean, "bool"),
            new LegacyNumericOperand("byte 8", (byte)8, TypeCode.Byte, "byte"),
            new LegacyNumericOperand("sbyte -2", (sbyte)-2, TypeCode.SByte, "sbyte"),
            new LegacyNumericOperand("short -3", (short)-3, TypeCode.Int16, "short"),
            new LegacyNumericOperand("ushort 4", (ushort)4, TypeCode.UInt16, "ushort"),
            new LegacyNumericOperand("int 5", 5, TypeCode.Int32, "int"),
            new LegacyNumericOperand("uint 6", (uint)6, TypeCode.UInt32, "uint"),
            new LegacyNumericOperand("long -7", (long)-7, TypeCode.Int64, "long"),
            new LegacyNumericOperand("ulong 8", (ulong)8, TypeCode.UInt64, "ulong"),
            new LegacyNumericOperand("float 1.5", 1.5f, TypeCode.Single, "float"),
            new LegacyNumericOperand("double 2.5", 2.5d, TypeCode.Double, "double"),
            new LegacyNumericOperand("decimal 3.5", 3.5m, TypeCode.Decimal, "decimal")
        };

        public static void VerifyPrimitiveArithmeticMatrix(string operationName, NumericOperation operation, LegacyNumbersOperator numbersOperator)
        {
            foreach (LegacyNumericOperand left in PrimitiveOperands)
            {
                foreach (LegacyNumericOperand right in PrimitiveOperands)
                {
                    string caseName = operationName + " with " + left.Name + " and " + right.Name;
                    LegacyNumericOutcome expectedOutcome = ExpectPrimitiveArithmetic(numbersOperator, left, right);

                    VerifyOutcome(caseName, operation, left.Value, right.Value, expectedOutcome);
                }
            }
        }

        public static void VerifyPrimitiveExtremaMatrix(string operationName, NumericOperation operation, LegacyNumbersOperator numbersOperator)
        {
            foreach (LegacyNumericOperand left in PrimitiveOperands)
            {
                foreach (LegacyNumericOperand right in PrimitiveOperands)
                {
                    string caseName = operationName + " with " + left.Name + " and " + right.Name;
                    LegacyNumericOutcome expectedOutcome = ExpectPrimitiveExtrema(numbersOperator, left, right);

                    VerifyOutcome(caseName, operation, left.Value, right.Value, expectedOutcome);
                }
            }
        }

        private static LegacyNumericOutcome ExpectPrimitiveArithmetic(LegacyNumbersOperator numbersOperator, LegacyNumericOperand left, LegacyNumericOperand right)
        {
            switch (numbersOperator)
            {
                case LegacyNumbersOperator.Add:
                    return ExpectAdd(left, right);
                case LegacyNumbersOperator.Soustract:
                    return ExpectSoustract(left, right);
                case LegacyNumbersOperator.Multiply:
                case LegacyNumbersOperator.Divide:
                case LegacyNumbersOperator.Modulo:
                    return ExpectMultiplyDivideOrModulo(numbersOperator, left, right);
                default:
                    throw new InvalidOperationException("Unsupported arithmetic operator: " + numbersOperator);
            }
        }

        private static LegacyNumericOutcome ExpectAdd(LegacyNumericOperand left, LegacyNumericOperand right)
        {
            if (left.TypeCode == TypeCode.Boolean)
            {
                return InvalidOperation(LegacyNumbersOperator.Add, "bool", right.TypeCode == TypeCode.Boolean ? "bool" : "byte");
            }

            if (right.TypeCode == TypeCode.Boolean)
            {
                return InvalidOperation(LegacyNumbersOperator.Add, left.TypeCode == TypeCode.UInt32 ? "unit" : left.LegacyTypeName, "bool");
            }

            if (IsSignedIntegral(left) && right.TypeCode == TypeCode.UInt64)
            {
                return InvalidOperation(LegacyNumbersOperator.Add, left.LegacyTypeName, "ulong");
            }

            if (left.TypeCode == TypeCode.UInt64 && IsSignedIntegral(right))
            {
                string rightTypeName = right.TypeCode == TypeCode.Int64 ? "ulong" : right.LegacyTypeName;
                return InvalidOperation(LegacyNumbersOperator.Add, "ulong", rightTypeName);
            }

            if (IsDecimalRealMix(left, right))
            {
                return LegacyNumericOutcome.Value(AddDecimalRealMix(left.Value, right.Value));
            }

            return LegacyNumericOutcome.Value(ApplyArithmetic(LegacyNumbersOperator.Add, left.Value, right.Value));
        }

        private static LegacyNumericOutcome ExpectSoustract(LegacyNumericOperand left, LegacyNumericOperand right)
        {
            if (left.TypeCode == TypeCode.Boolean)
            {
                return InvalidOperation(LegacyNumbersOperator.Soustract, "bool", right.TypeCode == TypeCode.Boolean ? "bool" : "byte");
            }

            if (right.TypeCode == TypeCode.Boolean)
            {
                return InvalidOperation(LegacyNumbersOperator.Soustract, left.LegacyTypeName, "bool");
            }

            if (right.TypeCode == TypeCode.Byte)
            {
                return LegacyNumericOutcome.Null();
            }

            if (IsSignedIntegral(left) && right.TypeCode == TypeCode.UInt64)
            {
                return InvalidOperation(LegacyNumbersOperator.Soustract, left.LegacyTypeName, "ulong");
            }

            if (left.TypeCode == TypeCode.UInt64 && IsSignedIntegral(right))
            {
                string rightTypeName = right.TypeCode == TypeCode.SByte ? "double" : right.LegacyTypeName;
                return InvalidOperation(LegacyNumbersOperator.Soustract, "ulong", rightTypeName);
            }

            if (IsDecimalRealMix(left, right))
            {
                return InvalidOperation(LegacyNumbersOperator.Soustract, left.LegacyTypeName, right.LegacyTypeName);
            }

            return LegacyNumericOutcome.Value(ApplyArithmetic(LegacyNumbersOperator.Soustract, left.Value, right.Value));
        }

        private static LegacyNumericOutcome ExpectMultiplyDivideOrModulo(LegacyNumbersOperator numbersOperator, LegacyNumericOperand left, LegacyNumericOperand right)
        {
            if (left.TypeCode == TypeCode.Boolean)
            {
                return LegacyNumericOutcome.Null();
            }

            if (right.TypeCode == TypeCode.Boolean)
            {
                return left.TypeCode == TypeCode.UInt64 && numbersOperator == LegacyNumbersOperator.Divide
                    ? InvalidOperationWithSymbol("-", "ulong", "bool")
                    : InvalidOperation(numbersOperator, left.LegacyTypeName, "bool");
            }

            if (right.TypeCode == TypeCode.Byte)
            {
                return LegacyNumericOutcome.Null();
            }

            if (IsSignedIntegral(left) && right.TypeCode == TypeCode.UInt64)
            {
                return InvalidOperation(numbersOperator, left.LegacyTypeName, "ulong");
            }

            if (left.TypeCode == TypeCode.UInt64 && IsSignedIntegral(right))
            {
                return InvalidOperation(numbersOperator, "ulong", right.LegacyTypeName);
            }

            if (IsDecimalRealMix(left, right))
            {
                string rightTypeName = numbersOperator == LegacyNumbersOperator.Modulo && left.TypeCode == TypeCode.Decimal && right.TypeCode == TypeCode.Double
                    ? "decimal"
                    : right.LegacyTypeName;

                return InvalidOperation(numbersOperator, left.LegacyTypeName, rightTypeName);
            }

            return LegacyNumericOutcome.Value(ApplyArithmetic(numbersOperator, left.Value, right.Value));
        }

        private static LegacyNumericOutcome ExpectPrimitiveExtrema(LegacyNumbersOperator numbersOperator, LegacyNumericOperand left, LegacyNumericOperand right)
        {
            if (left.TypeCode == TypeCode.Boolean)
            {
                return LegacyNumericOutcome.Null();
            }

            try
            {
                object convertedRight = Convert.ChangeType(right.Value, left.Value.GetType());
                object expectedValue = SelectExtrema(numbersOperator, left.Value, convertedRight);

                return LegacyNumericOutcome.Value(expectedValue);
            }
            catch (Exception exception)
            {
                return LegacyNumericOutcome.Exception(exception.GetType(), exception.Message);
            }
        }

        private static void VerifyOutcome(string caseName, NumericOperation operation, object left, object right, LegacyNumericOutcome expectedOutcome)
        {
            try
            {
                object actualValue = operation(left, right);

                if (expectedOutcome.ThrowsException)
                {
                    Assert.Fail(caseName + " should throw " + expectedOutcome.ExpectedExceptionType.Name);
                }

                if (expectedOutcome.ReturnsNull)
                {
                    Assert.IsNull(actualValue, caseName + " should return null");
                    return;
                }

                Assert.IsNotNull(actualValue, caseName + " should not return null");
                Assert.AreEqual(expectedOutcome.ExpectedValue.GetType(), actualValue.GetType(), caseName + " result type");
                Assert.AreEqual(expectedOutcome.ExpectedValue, actualValue, caseName + " result value");
            }
            catch (Exception exception)
            {
                if (!expectedOutcome.ThrowsException)
                {
                    Assert.Fail(caseName + " should not throw but threw " + exception.GetType().Name + ": " + exception.Message);
                }

                Assert.AreEqual(expectedOutcome.ExpectedExceptionType, exception.GetType(), caseName + " exception type");
                Assert.AreEqual(expectedOutcome.ExpectedExceptionMessage, exception.Message, caseName + " exception message");
            }
        }

        private static LegacyNumericOutcome InvalidOperation(LegacyNumbersOperator numbersOperator, string leftTypeName, string rightTypeName)
        {
            return InvalidOperationWithSymbol(SymbolFor(numbersOperator), leftTypeName, rightTypeName);
        }

        private static LegacyNumericOutcome InvalidOperationWithSymbol(string operatorSymbol, string leftTypeName, string rightTypeName)
        {
            return LegacyNumericOutcome.Exception(
                typeof(InvalidOperationException),
                "Operator '" + operatorSymbol + "' can't be applied to operands of types '" + leftTypeName + "' and '" + rightTypeName + "'");
        }

        private static string SymbolFor(LegacyNumbersOperator numbersOperator)
        {
            switch (numbersOperator)
            {
                case LegacyNumbersOperator.Add:
                    return "+";
                case LegacyNumbersOperator.Soustract:
                    return "-";
                case LegacyNumbersOperator.Multiply:
                    return "*";
                case LegacyNumbersOperator.Divide:
                    return "/";
                case LegacyNumbersOperator.Modulo:
                    return "%";
                default:
                    throw new InvalidOperationException("No arithmetic symbol for " + numbersOperator);
            }
        }

        private static bool IsSignedIntegral(LegacyNumericOperand operand)
        {
            return operand.TypeCode == TypeCode.SByte
                || operand.TypeCode == TypeCode.Int16
                || operand.TypeCode == TypeCode.Int32
                || operand.TypeCode == TypeCode.Int64;
        }

        private static bool IsDecimalRealMix(LegacyNumericOperand left, LegacyNumericOperand right)
        {
            return (left.TypeCode == TypeCode.Decimal && IsReal(right))
                || (right.TypeCode == TypeCode.Decimal && IsReal(left));
        }

        private static bool IsReal(LegacyNumericOperand operand)
        {
            return operand.TypeCode == TypeCode.Single || operand.TypeCode == TypeCode.Double;
        }

        private static object AddDecimalRealMix(object leftValue, object rightValue)
        {
            if (leftValue is decimal leftDecimal)
            {
                return leftDecimal + Convert.ToDecimal(rightValue);
            }

            return Convert.ToDecimal(leftValue) + (decimal)rightValue;
        }

        private static object ApplyArithmetic(LegacyNumbersOperator numbersOperator, object leftValue, object rightValue)
        {
            dynamic left = leftValue;
            dynamic right = rightValue;

            switch (numbersOperator)
            {
                case LegacyNumbersOperator.Add:
                    return left + right;
                case LegacyNumbersOperator.Soustract:
                    return left - right;
                case LegacyNumbersOperator.Multiply:
                    return left * right;
                case LegacyNumbersOperator.Divide:
                    return left / right;
                case LegacyNumbersOperator.Modulo:
                    return left % right;
                default:
                    throw new InvalidOperationException("Unsupported arithmetic operator: " + numbersOperator);
            }
        }

        private static object SelectExtrema(LegacyNumbersOperator numbersOperator, object leftValue, object convertedRight)
        {
            int comparison = ((IComparable)leftValue).CompareTo(convertedRight);

            switch (numbersOperator)
            {
                case LegacyNumbersOperator.Max:
                    return comparison >= 0 ? leftValue : convertedRight;
                case LegacyNumbersOperator.Min:
                    return comparison <= 0 ? leftValue : convertedRight;
                default:
                    throw new InvalidOperationException("Unsupported extrema operator: " + numbersOperator);
            }
        }
    }
}
