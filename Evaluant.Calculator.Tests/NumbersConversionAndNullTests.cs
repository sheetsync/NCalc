using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCalc.Tests
{
    // Characterizes conversion and null behavior around the numeric switchboard.
    [TestClass]
    public class NumbersConversionAndNullTests
    {
        [TestMethod]
        public void NumbersArithmetic_WithStringAndCharacterOperands_DoesParseOperandsAsDecimalsBeforeDispatch()
        {
            VerifyResults(
                new NumericResultCase("add character plus string as decimals", Numbers.Add, '5', "4", 9m, typeof(decimal)),
                new NumericResultCase("add string plus double through decimal conversion", Numbers.Add, "4", 2.5d, 6.5m, typeof(decimal)),
                new NumericResultCase("soustract character minus string as decimals", Numbers.Soustract, '5', "4", 1m, typeof(decimal)),
                new NumericResultCase("multiply character by string as decimals", Numbers.Multiply, '5', "4", 20m, typeof(decimal)),
                new NumericResultCase("divide character by string as decimals", Numbers.Divide, '5', "4", 1.25m, typeof(decimal)),
                new NumericResultCase("modulo character by string as decimals", Numbers.Modulo, '5', "4", 1m, typeof(decimal)),
                new NumericResultCase("modulo decimal by character as decimals", Numbers.Modulo, 3.5m, '5', 3.5m, typeof(decimal)));
        }

        [TestMethod]
        public void NumbersArithmetic_WithInvalidStringOperand_DoesThrowFormatExceptionBeforeDispatch()
        {
            VerifyExceptions(
                new NumericExceptionCase("add invalid string", Numbers.Add, "not-a-number", 1, typeof(FormatException), null),
                new NumericExceptionCase("soustract invalid string", Numbers.Soustract, "not-a-number", 1, typeof(FormatException), null),
                new NumericExceptionCase("multiply invalid string", Numbers.Multiply, "not-a-number", 1, typeof(FormatException), null),
                new NumericExceptionCase("divide invalid string", Numbers.Divide, "not-a-number", 1, typeof(FormatException), null),
                new NumericExceptionCase("modulo invalid string", Numbers.Modulo, "not-a-number", 1, typeof(FormatException), null),
                new NumericExceptionCase("max invalid right string parses before bool fallthrough", Numbers.Max, true, "not-a-number", typeof(FormatException), null),
                new NumericExceptionCase("min invalid left string parses before null handling", Numbers.Min, "not-a-number", null, typeof(FormatException), null));
        }

        [TestMethod]
        public void NumbersArithmetic_WithNullOperand_DoesThrowNullReferenceExceptionBeforeDispatch()
        {
            VerifyExceptions(
                new NumericExceptionCase("add null left", Numbers.Add, null, 1, typeof(NullReferenceException), null),
                new NumericExceptionCase("add null right", Numbers.Add, 1, null, typeof(NullReferenceException), null),
                new NumericExceptionCase("soustract null left", Numbers.Soustract, null, 1, typeof(NullReferenceException), null),
                new NumericExceptionCase("soustract null right", Numbers.Soustract, 1, null, typeof(NullReferenceException), null),
                new NumericExceptionCase("multiply null left", Numbers.Multiply, null, 1, typeof(NullReferenceException), null),
                new NumericExceptionCase("multiply null right", Numbers.Multiply, 1, null, typeof(NullReferenceException), null),
                new NumericExceptionCase("divide null left", Numbers.Divide, null, 1, typeof(NullReferenceException), null),
                new NumericExceptionCase("divide null right", Numbers.Divide, 1, null, typeof(NullReferenceException), null),
                new NumericExceptionCase("modulo null left", Numbers.Modulo, null, 1, typeof(NullReferenceException), null),
                new NumericExceptionCase("modulo null right", Numbers.Modulo, 1, null, typeof(NullReferenceException), null));
        }

        [TestMethod]
        public void NumbersExtrema_WithNullAndConvertedOperands_DoesReturnConvertedNonNullOperand()
        {
            VerifyResults(
                new NumericResultCase("max null string parses right before null return", Numbers.Max, null, "4", 4m, typeof(decimal)),
                new NumericResultCase("min null character parses right before null return", Numbers.Min, null, '5', 5m, typeof(decimal)),
                new NumericResultCase("max string null parses left before null return", Numbers.Max, "4", null, 4m, typeof(decimal)),
                new NumericResultCase("min character null parses left before null return", Numbers.Min, '5', null, 5m, typeof(decimal)),
                new NumericResultCase("max bool null returns bool before unsupported switch", Numbers.Max, true, null, true, typeof(bool)),
                new NumericResultCase("min null bool returns bool before unsupported switch", Numbers.Min, null, true, true, typeof(bool)),
                new NumericResultCase("max bool int still falls through", Numbers.Max, true, 1, null, null),
                new NumericResultCase("min bool int still falls through", Numbers.Min, true, 1, null, null));
        }

        private static void VerifyResults(params NumericResultCase[] cases)
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
