using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCalc.Tests
{
    [TestClass]
    public class NumbersInvalidOperationTests
    {
        [TestMethod]
        public void NumbersAdd_WithUnsupportedInputs_ThrowsCharacterizedExceptions()
        {
            VerifyAll(
                new NumericExceptionCase("bool plus bool", Numbers.Add, true, true, typeof(InvalidOperationException), "bool' and 'bool"),
                new NumericExceptionCase("bool plus decimal reports byte right side", Numbers.Add, true, 3.5m, typeof(InvalidOperationException), "bool' and 'byte"),
                new NumericExceptionCase("uint plus bool reports unit typo", Numbers.Add, (uint)6, true, typeof(InvalidOperationException), "unit' and 'bool"),
                new NumericExceptionCase("sbyte plus ulong", Numbers.Add, (sbyte)-2, (ulong)8, typeof(InvalidOperationException), "sbyte' and 'ulong"),
                new NumericExceptionCase("long plus ulong", Numbers.Add, (long)-7, (ulong)8, typeof(InvalidOperationException), "long' and 'ulong"),
                new NumericExceptionCase("non numeric string plus int", Numbers.Add, "not-a-number", 1, typeof(FormatException), null));
        }

        [TestMethod]
        public void NumbersSoustract_WithUnsupportedInputs_ThrowsCharacterizedExceptions()
        {
            VerifyAll(
                new NumericExceptionCase("bool minus bool", Numbers.Soustract, true, true, typeof(InvalidOperationException), "bool' and 'bool"),
                new NumericExceptionCase("sbyte minus ulong", Numbers.Soustract, (sbyte)-2, (ulong)8, typeof(InvalidOperationException), "sbyte' and 'ulong"),
                new NumericExceptionCase("ulong minus sbyte reports double typo", Numbers.Soustract, (ulong)8, (sbyte)-2, typeof(InvalidOperationException), "ulong' and 'double"),
                new NumericExceptionCase("single minus decimal", Numbers.Soustract, 1.5f, 3.5m, typeof(InvalidOperationException), "float' and 'decimal"),
                new NumericExceptionCase("decimal minus double", Numbers.Soustract, 3.5m, 2.5d, typeof(InvalidOperationException), "decimal' and 'double"));
        }

        [TestMethod]
        public void NumbersMultiply_WithUnsupportedInputs_ThrowsCharacterizedExceptions()
        {
            VerifyAll(
                new NumericExceptionCase("byte times bool", Numbers.Multiply, (byte)2, true, typeof(InvalidOperationException), "byte' and 'bool"),
                new NumericExceptionCase("sbyte times ulong", Numbers.Multiply, (sbyte)-2, (ulong)8, typeof(InvalidOperationException), "sbyte' and 'ulong"),
                new NumericExceptionCase("ulong times int", Numbers.Multiply, (ulong)8, 5, typeof(InvalidOperationException), "ulong' and 'int"),
                new NumericExceptionCase("single times decimal", Numbers.Multiply, 1.5f, 3.5m, typeof(InvalidOperationException), "float' and 'decimal"),
                new NumericExceptionCase("decimal times double", Numbers.Multiply, 3.5m, 2.5d, typeof(InvalidOperationException), "decimal' and 'double"));
        }

        [TestMethod]
        public void NumbersDivide_WithUnsupportedInputs_ThrowsCharacterizedExceptions()
        {
            VerifyAll(
                new NumericExceptionCase("byte divided by bool", Numbers.Divide, (byte)8, true, typeof(InvalidOperationException), "byte' and 'bool"),
                new NumericExceptionCase("ulong divided by bool reports minus operator", Numbers.Divide, (ulong)8, true, typeof(InvalidOperationException), "Operator '-'"),
                new NumericExceptionCase("int divided by ulong", Numbers.Divide, 8, (ulong)2, typeof(InvalidOperationException), "int' and 'ulong"),
                new NumericExceptionCase("single divided by decimal", Numbers.Divide, 1.5f, 3.5m, typeof(InvalidOperationException), "float' and 'decimal"),
                new NumericExceptionCase("double divided by decimal", Numbers.Divide, 2.5d, 3.5m, typeof(InvalidOperationException), "double' and 'decimal"));
        }

        [TestMethod]
        public void NumbersModulo_WithUnsupportedInputs_ThrowsCharacterizedExceptions()
        {
            VerifyAll(
                new NumericExceptionCase("byte modulo bool", Numbers.Modulo, (byte)8, true, typeof(InvalidOperationException), "byte' and 'bool"),
                new NumericExceptionCase("sbyte modulo ulong", Numbers.Modulo, (sbyte)-2, (ulong)8, typeof(InvalidOperationException), "sbyte' and 'ulong"),
                new NumericExceptionCase("ulong modulo int", Numbers.Modulo, (ulong)8, 5, typeof(InvalidOperationException), "ulong' and 'int"),
                new NumericExceptionCase("single modulo decimal", Numbers.Modulo, 1.5f, 3.5m, typeof(InvalidOperationException), "float' and 'decimal"),
                new NumericExceptionCase("decimal modulo double reports decimal twice", Numbers.Modulo, 3.5m, 2.5d, typeof(InvalidOperationException), "decimal' and 'decimal"));
        }

        private static void VerifyAll(params NumericExceptionCase[] cases)
        {
            foreach (NumericExceptionCase testCase in cases)
            {
                testCase.Verify();
            }
        }
    }
}
