using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Pafiso.Enums;
using Pafiso.Extensions;
using Pafiso.Mapping;
using Shouldly;

namespace Pafiso.EntityFrameworkCore.Tests;

public class InOperatorEfCoreTest {
    private SqliteConnection _connection = null!;
    private TestDbContext _context = null!;
    private FieldMapper<ProductDto, Product> _mapper = null!;

    [SetUp]
    public void Setup() {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Make SQLite LIKE case-sensitive to test case-insensitive In properly
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
    public void In_StringValues_MatchesMultiple() {
        var filter = Filter.WithMapper(
            "Name",
            FilterOperator.In,
            "Apple iPhone,OnePlus Nord",
            _mapper,
            EfCoreExpressionBuilder.Instance,
            caseSensitive: true);

        var results = _context.Products.AsQueryable()
            .Where(filter)
            .ToList();

        results.Count.ShouldBe(2);
        results.Select(p => p.Name).ShouldBe(new[] { "Apple iPhone", "OnePlus Nord" }, ignoreOrder: true);
    }

    [Test]
    public void NotIn_StringValues_ExcludesMatching() {
        var filter = Filter.WithMapper(
            "Name",
            FilterOperator.NotIn,
            "Apple iPhone,OnePlus Nord",
            _mapper,
            EfCoreExpressionBuilder.Instance,
            caseSensitive: true);

        var results = _context.Products.AsQueryable()
            .Where(filter)
            .ToList();

        results.Count.ShouldBe(3);
        results.ShouldAllBe(p => p.Name != "Apple iPhone" && p.Name != "OnePlus Nord");
    }

    [Test]
    public void In_CaseInsensitive_MatchesRegardlessOfCase() {
        var filter = Filter.WithMapper(
            "Name",
            FilterOperator.In,
            "apple iphone,samsung galaxy",
            _mapper,
            EfCoreExpressionBuilder.Instance,
            caseSensitive: false);

        var results = _context.Products.AsQueryable()
            .Where(filter)
            .ToList();

        results.Count.ShouldBe(2);
        results.Select(p => p.Name).ShouldBe(new[] { "Apple iPhone", "SAMSUNG Galaxy" }, ignoreOrder: true);
    }

    [Test]
    public void In_SingleValue_WorksLikeEquals() {
        var filter = Filter.WithMapper(
            "Name",
            FilterOperator.In,
            "google pixel",
            _mapper,
            EfCoreExpressionBuilder.Instance,
            caseSensitive: true);

        var results = _context.Products.AsQueryable()
            .Where(filter)
            .ToList();

        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("google pixel");
    }

    [Test]
    public void In_NoMatches_ReturnsEmpty() {
        var filter = Filter.WithMapper(
            "Name",
            FilterOperator.In,
            "NonExistent1,NonExistent2",
            _mapper,
            EfCoreExpressionBuilder.Instance,
            caseSensitive: true);

        var results = _context.Products.AsQueryable()
            .Where(filter)
            .ToList();

        results.Count.ShouldBe(0);
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
