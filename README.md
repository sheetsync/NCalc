# NCalc

NCalc is a small expression evaluator for .NET applications. It parses a string expression into an abstract syntax tree and evaluates it with the parameters, functions, and options supplied by the host application.

It is useful when an application needs user-editable rules, lightweight formulas, configuration-driven calculations, or small bits of business logic without compiling code at runtime.

## Install

```sh
dotnet add package NCalc
```

NCalc 2.x ships assets for:

- .NET Standard 2.0
- .NET Framework 3.5

## Quick Example

```csharp
using NCalc;

Expression expression = new Expression("2 * ([quantity] + 5)");
expression.Parameters["quantity"] = 3;

object result = expression.Evaluate();
Console.WriteLine(result); // 16
```

Parameters can be supplied directly:

```csharp
Expression expression = new Expression("[price] * [quantity]");
expression.Parameters["price"] = 12.5m;
expression.Parameters["quantity"] = 4;

object total = expression.Evaluate();
```

Functions can be handled lazily by the host application:

```csharp
Expression expression = new Expression("discount([subtotal])");
expression.Parameters["subtotal"] = 100m;

expression.EvaluateFunction += (name, args) =>
{
    if (name == "discount")
    {
        decimal subtotal = Convert.ToDecimal(args.Parameters[0].Evaluate());
        args.Result = subtotal * 0.9m;
    }
};

object discountedTotal = expression.Evaluate();
```

## Expression Features

NCalc supports common arithmetic, logical, comparison, bitwise, and conditional expressions, including:

- arithmetic operators such as `+`, `-`, `*`, `/`, and `%`
- comparison operators such as `=`, `!=`, `<`, `<=`, `>`, and `>=`
- logical operators such as `and`, `or`, and `not`
- ternary-style evaluation through `if(condition, whenTrue, whenFalse)`
- strings, numbers, booleans, dates, and parameters
- custom functions and parameter resolution callbacks

The evaluator preserves the long-standing NCalc behavior around numeric conversions, culture-sensitive parsing, exception messages, lazy parameter evaluation, and generated parser surface area. Those compatibility points are covered by the test suite because existing applications often rely on them in quiet, surprising ways.

## Compatibility Notes

NCalc 2.x is a major release because the strong-name signing key changed. The old NCalc 1.3.8 assembly used public key token `973cde3f1cafed03`; NCalc 2.x uses public key token `f59bbff0fd43f598`.

Source-compatible consumers can upgrade by rebuilding against the 2.x package. Already-compiled .NET Framework applications and libraries that reference the old strong-name identity must be recompiled; binding redirects cannot redirect one public key token to another.

The NuGet package keeps the ANTLR runtime surface embedded in `NCalc.dll`, matching the historical package shape. That means parser and lexer consumers can continue to use public types such as `NCalcLexer`, `NCalcParser`, and `Antlr.Runtime.Tree.CommonTree` from the package without adding a separate ANTLR dependency.

## Validation

The repository includes a local quality gate:

```sh
scripts/validate.sh
```

The gate builds the library, builds the solution, runs the modern test suite with coverage thresholds, runs framework compatibility checks, creates a signed package, checks package compatibility against NCalc 1.3.8, runs SDK package validation, and performs consumer smoke tests.

Release packaging also runs a Windows .NET Framework consumer smoke before publishing to NuGet.

## License

NCalc is released under the MIT License.
