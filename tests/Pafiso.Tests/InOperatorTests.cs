using NUnit.Framework;
using Pafiso.Enums;
using Pafiso.Util;
using Shouldly;

namespace Pafiso.Tests;

public class InOperatorTests {
    private class TestEntity {
        public string Category { get; set; } = string.Empty;
        public float Price { get; set; }
        public long Quantity { get; set; }
        public bool InStock { get; set; }
    }

    // ── SplitEscapedValues ───────────────────────────────────────────────────

    [Test]
    public void SplitEscapedValues_SimpleCommas_SplitsCorrectly() {
        var result = ExpressionUtilities.SplitEscapedValues("a,b,c");
        result.ShouldBe(new[] { "a", "b", "c" });
    }

    [Test]
    public void SplitEscapedValues_EscapedComma_PreservesLiteralComma() {
        var result = ExpressionUtilities.SplitEscapedValues(@"hello\,world,foo");
        result.ShouldBe(new[] { "hello,world", "foo" });
    }

    [Test]
    public void SplitEscapedValues_EscapedBackslash_PreservesLiteralBackslash() {
        var result = ExpressionUtilities.SplitEscapedValues(@"a\\b,c");
        result.ShouldBe(new[] { @"a\b", "c" });
    }

    [Test]
    public void SplitEscapedValues_SingleValue_ReturnsSingleItem() {
        var result = ExpressionUtilities.SplitEscapedValues("only");
        result.ShouldBe(new[] { "only" });
    }

    [Test]
    public void SplitEscapedValues_EmptyParts_PreservesEmptyStrings() {
        var result = ExpressionUtilities.SplitEscapedValues("a,,b");
        result.ShouldBe(new[] { "a", "", "b" });
    }

    // ── In with strings ──────────────────────────────────────────────────────

    [Test]
    public void In_StringValues_MatchesMultipleValues() {
        var expr = ExpressionUtilities.BuildFilterExpression<TestEntity>(
            nameof(TestEntity.Category), "x", FilterOperator.In, "Electronics,Books", true);

        var compiled = expr.Compile();
        compiled(new TestEntity { Category = "Electronics" }).ShouldBeTrue();
        compiled(new TestEntity { Category = "Books" }).ShouldBeTrue();
        compiled(new TestEntity { Category = "Toys" }).ShouldBeFalse();
    }

    [Test]
    public void NotIn_StringValues_ExcludesMatchingValues() {
        var expr = ExpressionUtilities.BuildFilterExpression<TestEntity>(
            nameof(TestEntity.Category), "x", FilterOperator.NotIn, "Electronics,Books", true);

        var compiled = expr.Compile();
        compiled(new TestEntity { Category = "Electronics" }).ShouldBeFalse();
        compiled(new TestEntity { Category = "Books" }).ShouldBeFalse();
        compiled(new TestEntity { Category = "Toys" }).ShouldBeTrue();
    }

    [Test]
    public void In_StringValues_CaseInsensitive_MatchesRegardlessOfCase() {
        var expr = ExpressionUtilities.BuildFilterExpression<TestEntity>(
            nameof(TestEntity.Category), "x", FilterOperator.In, "electronics,BOOKS", false);

        var compiled = expr.Compile();
        compiled(new TestEntity { Category = "Electronics" }).ShouldBeTrue();
        compiled(new TestEntity { Category = "Books" }).ShouldBeTrue();
        compiled(new TestEntity { Category = "Toys" }).ShouldBeFalse();
    }

    // ── In with numeric values ───────────────────────────────────────────────

    [Test]
    public void In_FloatValues_MatchesMultipleValues() {
        var expr = ExpressionUtilities.BuildFilterExpression<TestEntity>(
            nameof(TestEntity.Price), "x", FilterOperator.In, "10,20", true);

        var compiled = expr.Compile();
        compiled(new TestEntity { Price = 10f }).ShouldBeTrue();
        compiled(new TestEntity { Price = 20f }).ShouldBeTrue();
        compiled(new TestEntity { Price = 30f }).ShouldBeFalse();
    }

    [Test]
    public void In_LongValues_MatchesMultipleValues() {
        var expr = ExpressionUtilities.BuildFilterExpression<TestEntity>(
            nameof(TestEntity.Quantity), "x", FilterOperator.In, "10,20,30", true);

        var compiled = expr.Compile();
        compiled(new TestEntity { Quantity = 10 }).ShouldBeTrue();
        compiled(new TestEntity { Quantity = 20 }).ShouldBeTrue();
        compiled(new TestEntity { Quantity = 99 }).ShouldBeFalse();
    }

    // ── In with single value ─────────────────────────────────────────────────

    [Test]
    public void In_SingleValue_WorksLikeEquals() {
        var expr = ExpressionUtilities.BuildFilterExpression<TestEntity>(
            nameof(TestEntity.Category), "x", FilterOperator.In, "Electronics", true);

        var compiled = expr.Compile();
        compiled(new TestEntity { Category = "Electronics" }).ShouldBeTrue();
        compiled(new TestEntity { Category = "Books" }).ShouldBeFalse();
    }

    // ── In with null value ───────────────────────────────────────────────────

    [Test]
    public void In_NullValue_ReturnsFalse() {
        var expr = ExpressionUtilities.BuildFilterExpression<TestEntity>(
            nameof(TestEntity.Category), "x", FilterOperator.In, null, true);

        var compiled = expr.Compile();
        compiled(new TestEntity { Category = "Anything" }).ShouldBeFalse();
    }

    // ── In with escaped commas ───────────────────────────────────────────────

    [Test]
    public void In_EscapedCommaInValue_MatchesValueWithComma() {
        var expr = ExpressionUtilities.BuildFilterExpression<TestEntity>(
            nameof(TestEntity.Category), "x", FilterOperator.In, @"hello\,world,foo", true);

        var compiled = expr.Compile();
        compiled(new TestEntity { Category = "hello,world" }).ShouldBeTrue();
        compiled(new TestEntity { Category = "foo" }).ShouldBeTrue();
        compiled(new TestEntity { Category = "hello" }).ShouldBeFalse();
    }

    // ── Settings-based overload ──────────────────────────────────────────────

    [Test]
    public void In_WithSettings_StringValues_MatchesMultipleValues() {
        var expr = ExpressionUtilities.BuildFilterExpression<TestEntity>(
            nameof(TestEntity.Category), "x", FilterOperator.In, "Electronics,Books", true,
            PafisoSettings.Default);

        var compiled = expr.Compile();
        compiled(new TestEntity { Category = "Electronics" }).ShouldBeTrue();
        compiled(new TestEntity { Category = "Books" }).ShouldBeTrue();
        compiled(new TestEntity { Category = "Toys" }).ShouldBeFalse();
    }

    [Test]
    public void In_WithSettings_CaseInsensitive_MatchesRegardlessOfCase() {
        var expr = ExpressionUtilities.BuildFilterExpression<TestEntity>(
            nameof(TestEntity.Category), "x", FilterOperator.In, "electronics,BOOKS", false,
            PafisoSettings.Default);

        var compiled = expr.Compile();
        compiled(new TestEntity { Category = "Electronics" }).ShouldBeTrue();
        compiled(new TestEntity { Category = "Books" }).ShouldBeTrue();
        compiled(new TestEntity { Category = "Toys" }).ShouldBeFalse();
    }
}
