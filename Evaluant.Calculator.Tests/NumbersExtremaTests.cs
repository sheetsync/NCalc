using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCalc.Tests
{
    [TestClass]
    public class NumbersExtremaTests
    {
        [TestMethod]
        public void NumbersMax_WithNullInputs_ReturnsCharacterizedValues()
        {
            VerifyAll(
                new NumericResultCase("max null null", Numbers.Max, null, null, null, null),
                new NumericResultCase("max null int", Numbers.Max, null, 3, 3, typeof(int)),
                new NumericResultCase("max int null", Numbers.Max, 3, null, 3, typeof(int)));
        }

        [TestMethod]
        public void NumbersMin_WithNullInputs_ReturnsCharacterizedValues()
        {
            VerifyAll(
                new NumericResultCase("min null null", Numbers.Min, null, null, null, null),
                new NumericResultCase("min null int", Numbers.Min, null, 3, 3, typeof(int)),
                new NumericResultCase("min int null", Numbers.Min, 3, null, 3, typeof(int)));
        }

        [TestMethod]
        public void NumbersMax_UsesLeftOperandTypeForConversionAndResult()
        {
            VerifyAll(
                new NumericResultCase("max bool left returns null", Numbers.Max, true, 2, null, null),
                new NumericResultCase("max byte converts right to byte", Numbers.Max, (byte)2, 3.5m, (byte)4, typeof(byte)),
                new NumericResultCase("max sbyte converts string to sbyte", Numbers.Max, (sbyte)-2, "4", (sbyte)4, typeof(sbyte)),
                new NumericResultCase("max short converts char to short", Numbers.Max, (short)-3, '5', (short)5, typeof(short)),
                new NumericResultCase("max uint converts ulong to uint", Numbers.Max, (uint)6, (ulong)8, (uint)8, typeof(uint)),
                new NumericResultCase("max long converts bool to long", Numbers.Max, (long)-7, true, (long)1, typeof(long)),
                new NumericResultCase("max single converts decimal to single", Numbers.Max, 1.5f, 3.5m, 3.5f, typeof(float)),
                new NumericResultCase("max double converts char to double", Numbers.Max, 2.5d, '5', 5d, typeof(double)),
                new NumericResultCase("max decimal converts ulong to decimal", Numbers.Max, 3.5m, (ulong)8, 8m, typeof(decimal)),
                new NumericResultCase("max string parses left as decimal", Numbers.Max, "4", '5', 5m, typeof(decimal)));
        }

        [TestMethod]
        public void NumbersMin_UsesLeftOperandTypeForConversionAndResult()
        {
            VerifyAll(
                new NumericResultCase("min bool left returns null", Numbers.Min, true, 2, null, null),
                new NumericResultCase("min byte converts bool to byte", Numbers.Min, (byte)2, true, (byte)1, typeof(byte)),
                new NumericResultCase("min sbyte keeps left value", Numbers.Min, (sbyte)-2, "4", (sbyte)-2, typeof(sbyte)),
                new NumericResultCase("min ushort converts double to ushort", Numbers.Min, (ushort)4, 2.5d, (ushort)2, typeof(ushort)),
                new NumericResultCase("min int converts char to int", Numbers.Min, 5, '5', 5, typeof(int)),
                new NumericResultCase("min single keeps left value", Numbers.Min, 1.5f, 3.5m, 1.5f, typeof(float)),
                new NumericResultCase("min double keeps right converted value", Numbers.Min, 2.5d, 1.5f, 1.5d, typeof(double)),
                new NumericResultCase("min decimal converts bool to decimal", Numbers.Min, 3.5m, true, 1m, typeof(decimal)),
                new NumericResultCase("min string parses left as decimal", Numbers.Min, "4", '5', 4m, typeof(decimal)));
        }

        [TestMethod]
        public void NumbersExtrema_WithUnsupportedConversions_ThrowCharacterizedExceptions()
        {
            VerifyExceptions(
                new NumericExceptionCase("max byte with negative sbyte", Numbers.Max, (byte)2, (sbyte)-2, typeof(OverflowException), "unsigned byte"),
                new NumericExceptionCase("min byte with negative short", Numbers.Min, (byte)2, (short)-3, typeof(OverflowException), "unsigned byte"),
                new NumericExceptionCase("max ulong with negative long", Numbers.Max, (ulong)8, (long)-7, typeof(OverflowException), "UInt64"),
                new NumericExceptionCase("min ulong with negative sbyte", Numbers.Min, (ulong)8, (sbyte)-2, typeof(OverflowException), "UInt64"));
        }

        private static void VerifyAll(params NumericResultCase[] cases)
        {
            foreach (NumericResultCase testCase in cases)
            {
                testCase.Verify();
            }
        }

        private static void VerifyExceptions(params NumericExceptionCase[] cases)
        {
            foreach (NumericExceptionCase testCase in cases)
            {
                testCase.Verify();
            }
        }
    }
}
