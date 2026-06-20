using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCalc.Tests
{
    // Exercises the full primitive numeric switchboard without changing runtime behavior.
    [TestClass]
    public class NumbersPrimitiveMatrixTests
    {
        [TestMethod]
        public void NumbersAdd_WithPrimitiveNumericMatrix_DoesPreserveLegacyResultsAndExceptions()
        {
            LegacyNumbersMatrix.VerifyPrimitiveArithmeticMatrix("Numbers.Add", Numbers.Add, LegacyNumbersOperator.Add);
        }

        [TestMethod]
        public void NumbersSoustract_WithPrimitiveNumericMatrix_DoesPreserveLegacyResultsAndExceptions()
        {
            LegacyNumbersMatrix.VerifyPrimitiveArithmeticMatrix("Numbers.Soustract", Numbers.Soustract, LegacyNumbersOperator.Soustract);
        }

        [TestMethod]
        public void NumbersMultiply_WithPrimitiveNumericMatrix_DoesPreserveLegacyResultsNullsAndExceptions()
        {
            LegacyNumbersMatrix.VerifyPrimitiveArithmeticMatrix("Numbers.Multiply", Numbers.Multiply, LegacyNumbersOperator.Multiply);
        }

        [TestMethod]
        public void NumbersDivide_WithPrimitiveNumericMatrix_DoesPreserveLegacyResultsNullsAndExceptions()
        {
            LegacyNumbersMatrix.VerifyPrimitiveArithmeticMatrix("Numbers.Divide", Numbers.Divide, LegacyNumbersOperator.Divide);
        }

        [TestMethod]
        public void NumbersModulo_WithPrimitiveNumericMatrix_DoesPreserveLegacyResultsNullsAndExceptions()
        {
            LegacyNumbersMatrix.VerifyPrimitiveArithmeticMatrix("Numbers.Modulo", Numbers.Modulo, LegacyNumbersOperator.Modulo);
        }

        [TestMethod]
        public void NumbersMax_WithPrimitiveNumericMatrix_DoesPreserveLeftOperandConversionRules()
        {
            LegacyNumbersMatrix.VerifyPrimitiveExtremaMatrix("Numbers.Max", Numbers.Max, LegacyNumbersOperator.Max);
        }

        [TestMethod]
        public void NumbersMin_WithPrimitiveNumericMatrix_DoesPreserveLeftOperandConversionRules()
        {
            LegacyNumbersMatrix.VerifyPrimitiveExtremaMatrix("Numbers.Min", Numbers.Min, LegacyNumbersOperator.Min);
        }
    }
}
