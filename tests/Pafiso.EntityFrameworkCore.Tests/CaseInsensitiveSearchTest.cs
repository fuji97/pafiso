using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Pafiso.Enums;
using Pafiso.Extensions;
using Pafiso.Mapping;
using Shouldly;

namespace Pafiso.EntityFrameworkCore.Tests;

/// <summary>
/// Integration tests for case-insensitive filtering using EfCoreExpressionBuilder.
///
/// The bug: when caseSensitive=false, EfCoreExpressionBuilder lowercases the search value in
/// the LIKE pattern (e.g. "%samsung%") but never lowercases the column expression. On a
/// case-sensitive collation the comparison "SAMSUNG Galaxy" LIKE "%samsung%" returns false.
///
/// The SQLite :memory: database is configured with PRAGMA case_sensitive_like=ON to reproduce
/// the case-sensitive collation behaviour that SQL Server and other providers use by default.
/// </summary>
public class CaseInsensitiveSearchTest {
    private SqliteConnection _connection = null!;
    private TestDbContext _context = null!;
    private FieldMapper<ProductDto, Product> _mapper = null!;

    [SetUp]
    public void Setup() {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Make SQLite LIKE case-sensitive so the bug is observable
        using (var cmd = _connection.CreateCommand()) {
            cmd.CommandText = "PRAGMA case_sensitive_like=ON;";
            cmd.ExecuteNonQuery();
        }

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TestDbContext(options);
        _context.Database.EnsureCreated();

        _context.Products.AddRange(
            new Product { Id = 1, Name = "Apple iPhone" },
            new Product { Id = 2, Name = "SAMSUNG Galaxy" },
            new Product { Id = 3, Name = "google pixel" },
            new Product { Id = 4, Name = "OnePlus Nord" },
            new Product { Id = 5, Name = "Xiaomi Redmi" }
        );
        _context.SaveChanges();

        _mapper = new FieldMapper<ProductDto, Product>()
            .Map(dto => dto.Name, entity => entity.Name);
    }

    [TearDown]
    public void TearDown() {
        _context.Dispose();
        _connection.Dispose();
    }

    [Test]
    public void CaseInsensitiveContains_UpperCaseData_ShouldMatch() {
        // "samsung" should match "SAMSUNG Galaxy" case-insensitively
        var filter = Filter.WithMapper(
            "Name",
            FilterOperator.Contains,
            "samsung",
            _mapper,
            EfCoreExpressionBuilder.Instance,
            caseSensitive: false);

        var results = _context.Products.AsQueryable()
            .Where(filter)
            .ToList();

        results.Count.ShouldBe(1, "case-insensitive 'contains samsung' should match 'SAMSUNG Galaxy'");
        results[0].Name.ShouldBe("SAMSUNG Galaxy");
    }

    [Test]
    public void CaseInsensitiveContains_LowerCaseData_ShouldMatch() {
        // "PIXEL" should match "google pixel" case-insensitively
        var filter = Filter.WithMapper(
            "Name",
            FilterOperator.Contains,
            "PIXEL",
            _mapper,
            EfCoreExpressionBuilder.Instance,
            caseSensitive: false);

        var results = _context.Products.AsQueryable()
            .Where(filter)
            .ToList();

        results.Count.ShouldBe(1, "case-insensitive 'contains PIXEL' should match 'google pixel'");
        results[0].Name.ShouldBe("google pixel");
    }

    [Test]
    public void CaseInsensitiveStartsWith_MixedCaseData_ShouldMatch() {
        // "apple" should match "Apple iPhone" case-insensitively
        var filter = Filter.WithMapper(
            "Name",
            FilterOperator.StartsWith,
            "apple",
            _mapper,
            EfCoreExpressionBuilder.Instance,
            caseSensitive: false);

        var results = _context.Products.AsQueryable()
            .Where(filter)
            .ToList();

        results.Count.ShouldBe(1, "case-insensitive 'startsWith apple' should match 'Apple iPhone'");
        results[0].Name.ShouldBe("Apple iPhone");
    }

    [Test]
    public void CaseInsensitiveEndsWith_MixedCaseData_ShouldMatch() {
        // "NORD" should match "OnePlus Nord" case-insensitively
        var filter = Filter.WithMapper(
            "Name",
            FilterOperator.EndsWith,
            "NORD",
            _mapper,
            EfCoreExpressionBuilder.Instance,
            caseSensitive: false);

        var results = _context.Products.AsQueryable()
            .Where(filter)
            .ToList();

        results.Count.ShouldBe(1, "case-insensitive 'endsWith NORD' should match 'OnePlus Nord'");
        results[0].Name.ShouldBe("OnePlus Nord");
    }

    [Test]
    public void CaseInsensitiveEquals_MixedCaseData_ShouldMatch() {
        // "xiaomi redmi" should match "Xiaomi Redmi" case-insensitively
        var filter = Filter.WithMapper(
            "Name",
            FilterOperator.Equals,
            "xiaomi redmi",
            _mapper,
            EfCoreExpressionBuilder.Instance,
            caseSensitive: false);

        var results = _context.Products.AsQueryable()
            .Where(filter)
            .ToList();

        results.Count.ShouldBe(1, "case-insensitive 'equals xiaomi redmi' should match 'Xiaomi Redmi'");
        results[0].Name.ShouldBe("Xiaomi Redmi");
    }

    /// <summary>
    /// Verifies that the generated LIKE expression wraps the member in a ToLower call
    /// so that the database-side comparison is case-insensitive regardless of collation.
    ///
    /// The fix requires: EF.Functions.Like(x.Name.ToLower(), "%samsung%")
    /// The bug produces:  EF.Functions.Like(x.Name,            "%samsung%")
    /// </summary>
    [Test]
    public void CaseInsensitiveContains_LikeExpressionMustLowerTheMember() {
        Expression? capturedMember = null;

        Expression CapturingLikeBuilder(Expression member, string pattern) {
            capturedMember = member;
            return Expression.Constant(true); // value doesn't matter; we inspect the member
        }

        var param = Expression.Parameter(typeof(Product), "x");
        var memberExpr = Expression.Property(param, nameof(Product.Name));

        // Simulate what EfCoreExpressionUtilities does for caseSensitive=false Contains
        EfCoreExpressionBuilder.BuildLikeExpression(memberExpr, "%samsung%");

        // The real test: build through the full builder path with a fake Like function
        var builder = EfCoreExpressionBuilder.Instance;
        var lambda = builder.BuildFilterExpression<Product>(
            propName: nameof(Product.Name),
            paramName: "x",
            op: FilterOperator.Contains,
            value: "samsung",
            caseSensitive: false,
            settings: PafisoSettings.Default);

        // Walk the generated expression tree to find the Like call and inspect its second argument
        var likeCall = FindLikeCall(lambda.Body);
        likeCall.ShouldNotBeNull("expected an EF.Functions.Like call in the expression");

        // The second argument of Like(functions, matchExpression, pattern, escape) is the column.
        // For case-insensitive search it MUST be wrapped in ToLower/ToLowerInvariant.
        var columnArg = likeCall!.Arguments[1];
        columnArg.ShouldBeAssignableTo<MethodCallExpression>(
            "the column argument of EF.Functions.Like must be wrapped in ToLower() for " +
            "case-insensitive search to work on case-sensitive collations. " +
            $"Actual expression: {columnArg}");

        var toLowerCall = (MethodCallExpression)columnArg;
        toLowerCall.Method.Name.ShouldBeOneOf("ToLower", "ToLowerInvariant",
            "the column must be lowercased via ToLower or ToLowerInvariant");
    }

    private static System.Linq.Expressions.MethodCallExpression? FindLikeCall(Expression expr) {
        if (expr is MethodCallExpression call && call.Method.Name == "Like") return call;
        if (expr is UnaryExpression unary) return FindLikeCall(unary.Operand);
        if (expr is BinaryExpression bin) return FindLikeCall(bin.Left) ?? FindLikeCall(bin.Right);
        return null;
    }

    private class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options) {
        public DbSet<Product> Products { get; set; } = null!;
    }

    private class Product {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class ProductDto : MappingModel {
        public string? Name { get; set; }
    }
}
