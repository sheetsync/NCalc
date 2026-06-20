using System;

namespace NCalc.Play
{
    public static class Program
    {
        public static void Main()
        {
            string[] expressions =
            {
                "2 * (3 + 5)",
                "2 * (2*(2*(2+1)))",
                "10 % 3",
                "false || not (false and true)",
                "3 > 2 and 1 <= (3-2)",
                "3 % 2 != 10 % 3",
                "if([age] >= 18, 'adult', 'minor')",
            };

            foreach (string expressionText in expressions)
            {
                Expression expression = new Expression(expressionText);
                expression.Parameters["age"] = 42;

                Console.WriteLine("{0} = {1}", expressionText, expression.Evaluate());
            }
        }
    }
}
