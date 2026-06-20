using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NCalc.Domain;
using DomainValueType = NCalc.Domain.ValueType;

namespace NCalc.Tests
{
    /// <summary>
    /// Characterizes the public AST/domain model shared by parser, serializer, and evaluator.
    /// </summary>
    [TestClass]
    public class DomainPublicSurfaceTests
    {
        [TestMethod]
        public void BinaryExpression_WithConstructorArguments_ExposesMutableProperties()
        {
            LogicalExpression leftExpression = new ValueExpression(1);
            LogicalExpression rightExpression = new ValueExpression(2);
            var expression = new BinaryExpression(BinaryExpressionType.Plus, leftExpression, rightExpression);

            Assert.AreEqual(BinaryExpressionType.Plus, expression.Type);
            Assert.AreSame(leftExpression, expression.LeftExpression);
            Assert.AreSame(rightExpression, expression.RightExpression);

            LogicalExpression replacementLeftExpression = new Identifier("left");
            LogicalExpression replacementRightExpression = new Identifier("right");
            expression.Type = BinaryExpressionType.Minus;
            expression.LeftExpression = replacementLeftExpression;
            expression.RightExpression = replacementRightExpression;

            Assert.AreEqual(BinaryExpressionType.Minus, expression.Type);
            Assert.AreSame(replacementLeftExpression, expression.LeftExpression);
            Assert.AreSame(replacementRightExpression, expression.RightExpression);
        }

        [TestMethod]
        public void UnaryExpression_WithConstructorArguments_ExposesMutableProperties()
        {
            LogicalExpression innerExpression = new ValueExpression(1);
            var expression = new UnaryExpression(UnaryExpressionType.Negate, innerExpression);

            Assert.AreEqual(UnaryExpressionType.Negate, expression.Type);
            Assert.AreSame(innerExpression, expression.Expression);

            LogicalExpression replacementExpression = new Identifier("flag");
            expression.Type = UnaryExpressionType.Not;
            expression.Expression = replacementExpression;

            Assert.AreEqual(UnaryExpressionType.Not, expression.Type);
            Assert.AreSame(replacementExpression, expression.Expression);
        }

        [TestMethod]
        public void TernaryExpression_WithConstructorArguments_ExposesMutableProperties()
        {
            LogicalExpression conditionExpression = new Identifier("condition");
            LogicalExpression trueExpression = new ValueExpression(1);
            LogicalExpression falseExpression = new ValueExpression(0);
            var expression = new TernaryExpression(conditionExpression, trueExpression, falseExpression);

            Assert.AreSame(conditionExpression, expression.LeftExpression);
            Assert.AreSame(trueExpression, expression.MiddleExpression);
            Assert.AreSame(falseExpression, expression.RightExpression);

            LogicalExpression replacementConditionExpression = new ValueExpression(true);
            LogicalExpression replacementTrueExpression = new Identifier("trueValue");
            LogicalExpression replacementFalseExpression = new Identifier("falseValue");
            expression.LeftExpression = replacementConditionExpression;
            expression.MiddleExpression = replacementTrueExpression;
            expression.RightExpression = replacementFalseExpression;

            Assert.AreSame(replacementConditionExpression, expression.LeftExpression);
            Assert.AreSame(replacementTrueExpression, expression.MiddleExpression);
            Assert.AreSame(replacementFalseExpression, expression.RightExpression);
        }

        [TestMethod]
        public void Function_WithConstructorArguments_ExposesMutableProperties()
        {
            var identifier = new Identifier("Sum");
            var arguments = new LogicalExpression[]
            {
                new ValueExpression(1),
                new ValueExpression(2)
            };
            var function = new Function(identifier, arguments);

            Assert.AreSame(identifier, function.Identifier);
            Assert.AreSame(arguments, function.Expressions);

            var replacementIdentifier = new Identifier("Max");
            var replacementArguments = new LogicalExpression[]
            {
                new Identifier("amount"),
                new ValueExpression(10)
            };
            function.Identifier = replacementIdentifier;
            function.Expressions = replacementArguments;

            Assert.AreSame(replacementIdentifier, function.Identifier);
            Assert.AreSame(replacementArguments, function.Expressions);
        }

        [TestMethod]
        public void Identifier_WithConstructorArguments_ExposesMutableProperties()
        {
            var identifier = new Identifier("original");

            Assert.AreEqual("original", identifier.Name);

            identifier.Name = "replacement";

            Assert.AreEqual("replacement", identifier.Name);
        }

        [TestMethod]
        public void ValueExpression_WithExplicitTypeConstructor_ExposesMutableProperties()
        {
            var expression = new ValueExpression("123", DomainValueType.Integer);

            Assert.AreEqual("123", expression.Value);
            Assert.AreEqual(DomainValueType.Integer, expression.Type);

            expression.Value = "abc";
            expression.Type = DomainValueType.String;

            Assert.AreEqual("abc", expression.Value);
            Assert.AreEqual(DomainValueType.String, expression.Type);
        }

        [TestMethod]
        public void ValueExpression_WithStronglyTypedConstructors_InferExpectedTypes()
        {
            DateTime dateTime = new DateTime(2026, 6, 20);

            AssertValueExpression(new ValueExpression("hello"), "hello", DomainValueType.String);
            AssertValueExpression(new ValueExpression(42), 42, DomainValueType.Integer);
            AssertValueExpression(new ValueExpression(1.5F), 1.5F, DomainValueType.Float);
            AssertValueExpression(new ValueExpression(dateTime), dateTime, DomainValueType.DateTime);
            AssertValueExpression(new ValueExpression(true), true, DomainValueType.Boolean);
        }

        [TestMethod]
        public void ValueExpression_WithObjectConstructor_InferExpectedTypes()
        {
            object[,] cases =
            {
                { true, DomainValueType.Boolean },
                { new DateTime(2026, 6, 20), DomainValueType.DateTime },
                { 1.5M, DomainValueType.Float },
                { 1.5D, DomainValueType.Float },
                { 1.5F, DomainValueType.Float },
                { (byte)1, DomainValueType.Integer },
                { (sbyte)1, DomainValueType.Integer },
                { (short)1, DomainValueType.Integer },
                { 1, DomainValueType.Integer },
                { 1L, DomainValueType.Integer },
                { (ushort)1, DomainValueType.Integer },
                { 1U, DomainValueType.Integer },
                { 1UL, DomainValueType.Integer },
                { "hello", DomainValueType.String }
            };

            for (int index = 0; index < cases.GetLength(0); index++)
            {
                object value = cases[index, 0];
                var expression = new ValueExpression(value);

                Assert.AreSame(value, expression.Value);
                Assert.AreEqual(cases[index, 1], expression.Type);
            }
        }

        [TestMethod]
        public void ValueExpression_WithUnsupportedObject_ThrowsEvaluationException()
        {
            var unsupportedValue = new object();

            EvaluationException exception = TestSupport.Throws<EvaluationException>(
                delegate { new ValueExpression(unsupportedValue); });

            Assert.AreEqual("This value could not be handled: " + unsupportedValue, exception.Message);
        }

        [TestMethod]
        public void Accept_WithConcreteExpressions_DispatchesToSpecificVisitorOverload()
        {
            AssertVisit(
                new BinaryExpression(BinaryExpressionType.Plus, new ValueExpression(1), new ValueExpression(2)),
                typeof(BinaryExpression));
            AssertVisit(
                new UnaryExpression(UnaryExpressionType.Negate, new ValueExpression(1)),
                typeof(UnaryExpression));
            AssertVisit(
                new TernaryExpression(new ValueExpression(true), new ValueExpression(1), new ValueExpression(0)),
                typeof(TernaryExpression));
            AssertVisit(new ValueExpression(1), typeof(ValueExpression));
            AssertVisit(
                new Function(new Identifier("Sum"), new LogicalExpression[] { new ValueExpression(1) }),
                typeof(Function));
            AssertVisit(new Identifier("amount"), typeof(Identifier));
        }

        [TestMethod]
        public void Accept_WithBareLogicalExpression_DispatchesToBaseVisitorOverload()
        {
            LogicalExpression expression = new BareLogicalExpression();

            AssertVisit(expression, typeof(LogicalExpression));
        }

        [TestMethod]
        public void FluentMethods_WithLogicalExpressionOperands_CreateBinaryExpressionsWithExpectedShape()
        {
            foreach (FluentBinaryExpressionCase expressionCase in GetFluentBinaryExpressionCases())
            {
                LogicalExpression leftExpression = new Identifier("left");
                LogicalExpression rightExpression = new Identifier("right");

                BinaryExpression expression = expressionCase.CreateWithLogicalOperand(leftExpression, rightExpression);

                Assert.AreEqual(expressionCase.ExpectedType, expression.Type);
                Assert.AreSame(leftExpression, expression.LeftExpression);
                Assert.AreSame(rightExpression, expression.RightExpression);
            }
        }

        [TestMethod]
        public void FluentMethods_WithObjectOperands_WrapRightOperandAsValueExpression()
        {
            foreach (FluentBinaryExpressionCase expressionCase in GetFluentBinaryExpressionCases())
            {
                LogicalExpression leftExpression = new Identifier("left");

                BinaryExpression expression = expressionCase.CreateWithValueOperand(leftExpression, 2);

                Assert.AreEqual(expressionCase.ExpectedType, expression.Type);
                Assert.AreSame(leftExpression, expression.LeftExpression);
                Assert.IsInstanceOfType(expression.RightExpression, typeof(ValueExpression));

                var rightExpression = (ValueExpression)expression.RightExpression;
                Assert.AreEqual(2, rightExpression.Value);
                Assert.AreEqual(DomainValueType.Integer, rightExpression.Type);
            }
        }

        [TestMethod]
        public void ExpressionEvaluation_WithManualDomainTree_EvaluatesParametersFunctionsAndBranches()
        {
            LogicalExpression conditionExpression = new Identifier("amount").GreaterThan(100);
            LogicalExpression trueExpression = new Function(
                new Identifier("Max"),
                new LogicalExpression[]
                {
                    new Identifier("amount"),
                    new ValueExpression(250)
                });
            LogicalExpression falseExpression = new ValueExpression(0);
            var expression = new Expression(new TernaryExpression(conditionExpression, trueExpression, falseExpression));

            expression.Parameters["amount"] = 125;
            Assert.AreEqual(250, expression.Evaluate());

            expression.Parameters["amount"] = 75;
            Assert.AreEqual(0, expression.Evaluate());
        }

        private static void AssertValueExpression(ValueExpression expression, object expectedValue, DomainValueType expectedType)
        {
            Assert.AreEqual(expectedValue, expression.Value);
            Assert.AreEqual(expectedType, expression.Type);
        }

        private static void AssertVisit(LogicalExpression expression, Type expectedVisitType)
        {
            var visitor = new RecordingLogicalExpressionVisitor();

            expression.Accept(visitor);

            Assert.AreEqual(1, visitor.VisitCount);
            Assert.AreSame(expression, visitor.LastExpression);
            Assert.AreEqual(expectedVisitType, visitor.LastVisitType);
        }

        private static IEnumerable<FluentBinaryExpressionCase> GetFluentBinaryExpressionCases()
        {
            yield return CreateFluentBinaryExpressionCase(
                BinaryExpressionType.And,
                (left, right) => left.And(right),
                (left, value) => left.And(value));
            yield return CreateFluentBinaryExpressionCase(
                BinaryExpressionType.Or,
                (left, right) => left.Or(right),
                (left, value) => left.Or(value));
            yield return CreateFluentBinaryExpressionCase(
                BinaryExpressionType.Equal,
                (left, right) => left.EqualsTo(right),
                (left, value) => left.EqualsTo(value));
            yield return CreateFluentBinaryExpressionCase(
                BinaryExpressionType.NotEqual,
                (left, right) => left.NotEqual(right),
                (left, value) => left.NotEqual(value));
            yield return CreateFluentBinaryExpressionCase(
                BinaryExpressionType.Greater,
                (left, right) => left.GreaterThan(right),
                (left, value) => left.GreaterThan(value));
            yield return CreateFluentBinaryExpressionCase(
                BinaryExpressionType.GreaterOrEqual,
                (left, right) => left.GreaterOrEqualThan(right),
                (left, value) => left.GreaterOrEqualThan(value));
            yield return CreateFluentBinaryExpressionCase(
                BinaryExpressionType.Lesser,
                (left, right) => left.LesserThan(right),
                (left, value) => left.LesserThan(value));
            yield return CreateFluentBinaryExpressionCase(
                BinaryExpressionType.LesserOrEqual,
                (left, right) => left.LesserOrEqualThan(right),
                (left, value) => left.LesserOrEqualThan(value));
            yield return CreateFluentBinaryExpressionCase(
                BinaryExpressionType.Minus,
                (left, right) => left.Minus(right),
                (left, value) => left.Minus(value));
            yield return CreateFluentBinaryExpressionCase(
                BinaryExpressionType.Plus,
                (left, right) => left.Plus(right),
                (left, value) => left.Plus(value));
            yield return CreateFluentBinaryExpressionCase(
                BinaryExpressionType.Times,
                (left, right) => left.Mult(right),
                (left, value) => left.Mult(value));
            yield return CreateFluentBinaryExpressionCase(
                BinaryExpressionType.Div,
                (left, right) => left.DividedBy(right),
                (left, value) => left.DividedBy(value));
            yield return CreateFluentBinaryExpressionCase(
                BinaryExpressionType.Modulo,
                (left, right) => left.Modulo(right),
                (left, value) => left.Modulo(value));
            yield return CreateFluentBinaryExpressionCase(
                BinaryExpressionType.BitwiseOr,
                (left, right) => left.BitwiseOr(right),
                (left, value) => left.BitwiseOr(value));
            yield return CreateFluentBinaryExpressionCase(
                BinaryExpressionType.BitwiseAnd,
                (left, right) => left.BitwiseAnd(right),
                (left, value) => left.BitwiseAnd(value));
            yield return CreateFluentBinaryExpressionCase(
                BinaryExpressionType.BitwiseXOr,
                (left, right) => left.BitwiseXOr(right),
                (left, value) => left.BitwiseXOr(value));
            yield return CreateFluentBinaryExpressionCase(
                BinaryExpressionType.LeftShift,
                (left, right) => left.LeftShift(right),
                (left, value) => left.LeftShift(value));
            yield return CreateFluentBinaryExpressionCase(
                BinaryExpressionType.RightShift,
                (left, right) => left.RightShift(right),
                (left, value) => left.RightShift(value));
        }

        private static FluentBinaryExpressionCase CreateFluentBinaryExpressionCase(
            BinaryExpressionType expectedType,
            LogicalOperandFactory logicalOperandFactory,
            ValueOperandFactory valueOperandFactory)
        {
            return new FluentBinaryExpressionCase(expectedType, logicalOperandFactory, valueOperandFactory);
        }

        private delegate BinaryExpression LogicalOperandFactory(LogicalExpression leftExpression, LogicalExpression rightExpression);

        private delegate BinaryExpression ValueOperandFactory(LogicalExpression leftExpression, object rightValue);

        private sealed class FluentBinaryExpressionCase
        {
            public FluentBinaryExpressionCase(
                BinaryExpressionType expectedType,
                LogicalOperandFactory logicalOperandFactory,
                ValueOperandFactory valueOperandFactory)
            {
                ExpectedType = expectedType;
                LogicalOperandFactory = logicalOperandFactory;
                ValueOperandFactory = valueOperandFactory;
            }

            public BinaryExpressionType ExpectedType { get; private set; }

            private LogicalOperandFactory LogicalOperandFactory { get; set; }

            private ValueOperandFactory ValueOperandFactory { get; set; }

            public BinaryExpression CreateWithLogicalOperand(LogicalExpression leftExpression, LogicalExpression rightExpression)
            {
                return LogicalOperandFactory(leftExpression, rightExpression);
            }

            public BinaryExpression CreateWithValueOperand(LogicalExpression leftExpression, object rightValue)
            {
                return ValueOperandFactory(leftExpression, rightValue);
            }
        }

        private sealed class RecordingLogicalExpressionVisitor : LogicalExpressionVisitor
        {
            public int VisitCount { get; private set; }

            public Type LastVisitType { get; private set; }

            public LogicalExpression LastExpression { get; private set; }

            public override void Visit(LogicalExpression expression)
            {
                RecordVisit(typeof(LogicalExpression), expression);
            }

            public override void Visit(TernaryExpression expression)
            {
                RecordVisit(typeof(TernaryExpression), expression);
            }

            public override void Visit(BinaryExpression expression)
            {
                RecordVisit(typeof(BinaryExpression), expression);
            }

            public override void Visit(UnaryExpression expression)
            {
                RecordVisit(typeof(UnaryExpression), expression);
            }

            public override void Visit(ValueExpression expression)
            {
                RecordVisit(typeof(ValueExpression), expression);
            }

            public override void Visit(Function function)
            {
                RecordVisit(typeof(Function), function);
            }

            public override void Visit(Identifier function)
            {
                RecordVisit(typeof(Identifier), function);
            }

            private void RecordVisit(Type visitType, LogicalExpression expression)
            {
                VisitCount++;
                LastVisitType = visitType;
                LastExpression = expression;
            }
        }

        private sealed class BareLogicalExpression : LogicalExpression
        {
        }
    }
}
