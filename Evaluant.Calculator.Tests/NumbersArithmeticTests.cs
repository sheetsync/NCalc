using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCalc.Tests
{
    [TestClass]
    public class NumbersArithmeticTests
    {
        [TestMethod]
        public void NumbersAdd_WithMixedNumericInputs_ReturnsCharacterizedValues()
        {
            VerifyAll(
                new NumericResultCase("byte plus byte", Numbers.Add, (byte)2, (byte)2, 4, typeof(int)),
                new NumericResultCase("byte plus uint", Numbers.Add, (byte)2, (uint)6, (uint)8, typeof(uint)),
                new NumericResultCase("byte plus ulong", Numbers.Add, (byte)2, (ulong)8, (ulong)10, typeof(ulong)),
                new NumericResultCase("short plus single", Numbers.Add, (short)-3, 1.5f, -1.5f, typeof(float)),
                new NumericResultCase("int plus double", Numbers.Add, 5, 2.5d, 7.5d, typeof(double)),
                new NumericResultCase("long plus decimal", Numbers.Add, (long)-7, 3.5m, -3.5m, typeof(decimal)),
                new NumericResultCase("single plus decimal", Numbers.Add, 1.5f, 3.5m, 5.0m, typeof(decimal)),
                new NumericResultCase("double plus decimal", Numbers.Add, 2.5d, 3.5m, 6.0m, typeof(decimal)),
                new NumericResultCase("string plus int", Numbers.Add, "4", 5, 9m, typeof(decimal)),
                new NumericResultCase("char plus decimal", Numbers.Add, '5', 3.5m, 8.5m, typeof(decimal)));
        }

        [TestMethod]
        public void NumbersSoustract_WithMixedNumericInputs_ReturnsCharacterizedValues()
        {
            VerifyAll(
                new NumericResultCase("byte minus byte falls through", Numbers.Soustract, (byte)2, (byte)2, null, null),
                new NumericResultCase("int minus byte falls through", Numbers.Soustract, 5, (byte)2, null, null),
                new NumericResultCase("byte minus sbyte", Numbers.Soustract, (byte)2, (sbyte)-2, 4, typeof(int)),
                new NumericResultCase("byte minus uint wraps", Numbers.Soustract, (byte)2, (uint)6, UInt32.MaxValue - 3, typeof(uint)),
                new NumericResultCase("byte minus ulong wraps", Numbers.Soustract, (byte)2, (ulong)8, UInt64.MaxValue - 5, typeof(ulong)),
                new NumericResultCase("uint minus ulong wraps", Numbers.Soustract, (uint)6, (ulong)8, UInt64.MaxValue - 1, typeof(ulong)),
                new NumericResultCase("string minus char", Numbers.Soustract, "4", '5', -1m, typeof(decimal)));
        }

        [TestMethod]
        public void NumbersMultiply_WithMixedNumericInputs_ReturnsCharacterizedValues()
        {
            VerifyAll(
                new NumericResultCase("byte times byte falls through", Numbers.Multiply, (byte)2, (byte)2, null, null),
                new NumericResultCase("byte times sbyte", Numbers.Multiply, (byte)2, (sbyte)-2, -4, typeof(int)),
                new NumericResultCase("ushort times ulong", Numbers.Multiply, (ushort)4, (ulong)8, (ulong)32, typeof(ulong)),
                new NumericResultCase("int times double", Numbers.Multiply, 5, 2.5d, 12.5d, typeof(double)),
                new NumericResultCase("long times decimal", Numbers.Multiply, (long)-7, 3.5m, -24.5m, typeof(decimal)),
                new NumericResultCase("string times char", Numbers.Multiply, "4", '5', 20m, typeof(decimal)));
        }

        [TestMethod]
        public void NumbersDivide_WithMixedNumericInputs_ReturnsCharacterizedValues()
        {
            VerifyAll(
                new NumericResultCase("byte divided by byte falls through", Numbers.Divide, (byte)8, (byte)2, null, null),
                new NumericResultCase("byte divided by sbyte", Numbers.Divide, (byte)8, (sbyte)2, 4, typeof(int)),
                new NumericResultCase("int divided by int uses integer division", Numbers.Divide, 5, 2, 2, typeof(int)),
                new NumericResultCase("int divided by double", Numbers.Divide, 5, 2.5d, 2d, typeof(double)),
                new NumericResultCase("uint divided by ulong", Numbers.Divide, (uint)8, (ulong)2, (ulong)4, typeof(ulong)),
                new NumericResultCase("decimal divided by int", Numbers.Divide, 3.5m, 5, 0.7m, typeof(decimal)),
                new NumericResultCase("string divided by char", Numbers.Divide, "8", '4', 2m, typeof(decimal)));
        }

        [TestMethod]
        public void NumbersModulo_WithMixedNumericInputs_ReturnsCharacterizedValues()
        {
            VerifyAll(
                new NumericResultCase("byte modulo byte falls through", Numbers.Modulo, (byte)8, (byte)2, null, null),
                new NumericResultCase("byte modulo sbyte", Numbers.Modulo, (byte)8, (sbyte)3, 2, typeof(int)),
                new NumericResultCase("int modulo int", Numbers.Modulo, 5, 2, 1, typeof(int)),
                new NumericResultCase("uint modulo ulong", Numbers.Modulo, (uint)8, (ulong)3, (ulong)2, typeof(ulong)),
                new NumericResultCase("double modulo single", Numbers.Modulo, 5.5d, 2.0f, 1.5d, typeof(double)),
                new NumericResultCase("decimal modulo int", Numbers.Modulo, 7.5m, 2, 1.5m, typeof(decimal)),
                new NumericResultCase("string modulo char", Numbers.Modulo, "9", '4', 1m, typeof(decimal)));
        }

        private static void VerifyAll(params NumericResultCase[] cases)
        {
            foreach (NumericResultCase testCase in cases)
            {
                testCase.Verify();
            }
        }
    }
}
