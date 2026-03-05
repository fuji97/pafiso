using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using Pafiso.EntityFrameworkCore;
using Pafiso.Enums;
using Pafiso.Mapping;
using Shouldly;

namespace Pafiso.Integration.Tests;

public class InOperatorIntegrationTest {
    // ── DTOs ──────────────────────────────────────────────────────────────────

    public class ProductFilterDto : MappingModel {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Category { get; set; }
        public decimal Price { get; set; }
        public bool InStock { get; set; }
    }

    // ── Entity ────────────────────────────────────────────────────────────────

    private class Product {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool InStock { get; set; }
    }

    // ── DbContext ─────────────────────────────────────────────────────────────

    private class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options) {
        public DbSet<Product> Products { get; set; } = null!;
    }

    // ── Infrastructure ────────────────────────────────────────────────────────

    private SqliteConnection _connection = null!;
    private TestDbContext _context = null!;

    [SetUp]
    public void Setup() {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TestDbContext(options);
        _context.Database.EnsureCreated();

        _context.Products.AddRange(
            new Product { Id = 1, Name = "Apple iPhone",   Category = "Electronics", Price = 999m,  InStock = true  },
            new Product { Id = 2, Name = "Samsung Galaxy",  Category = "Electronics", Price = 799m,  InStock = true  },
            new Product { Id = 3, Name = "Clean Code",      Category = "Books",       Price = 35m,   InStock = true  },
            new Product { Id = 4, Name = "The Pragmatic Programmer", Category = "Books", Price = 45m, InStock = false },
            new Product { Id = 5, Name = "Sony Headphones", Category = "Electronics", Price = 299m,  InStock = false },
            new Product { Id = 6, Name = "Kotlin in Action", Category = "Books",      Price = 55m,   InStock = true  },
            new Product { Id = 7, Name = "Logitech Mouse",  Category = "Accessories", Price = 79m,   InStock = true  },
            new Product { Id = 8, Name = "Mechanical Keyboard", Category = "Accessories", Price = 149m, InStock = false }
        );
        _context.SaveChanges();
    }

    [TearDown]
    public void TearDown() {
        _context.Dispose();
        _connection.Dispose();
    }

    private static IQueryCollection ToQueryCollection(IDictionary<string, string> dict) {
        return new QueryCollection(
            dict.ToDictionary(kvp => kvp.Key, kvp => new StringValues(kvp.Value)));
    }

    // ── In – strings ─────────────────────────────────────────────────────────

    [Test]
    public async Task In_StringValues_ReturnsMatchingCategories() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"]     = "in",
            ["filters[0][val]"]    = "Electronics,Books"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance))
            .ToPagedListAsync();

        result.TotalEntries.ShouldBe(6); // 3 Electronics + 3 Books
        result.ShouldAllBe(p => p.Category == "Electronics" || p.Category == "Books");
    }

    [Test]
    public async Task NotIn_StringValues_ExcludesMatchingCategories() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"]     = "notin",
            ["filters[0][val]"]    = "Electronics,Books"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance))
            .ToPagedListAsync();

        result.TotalEntries.ShouldBe(2); // 2 Accessories
        result.ShouldAllBe(p => p.Category == "Accessories");
    }

    // ── In – case-insensitive strings ────────────────────────────────────────

    [Test]
    public async Task In_CaseInsensitive_MatchesRegardlessOfCase() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"]     = "in",
            ["filters[0][val]"]    = "electronics,books",
            ["filters[0][case]"]   = "false"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance))
            .ToPagedListAsync();

        result.TotalEntries.ShouldBe(6);
        result.ShouldAllBe(p => p.Category == "Electronics" || p.Category == "Books");
    }

    // ── In – numeric values ──────────────────────────────────────────────────

    [Test]
    public async Task In_NumericValues_MatchesById() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "ProductId",
            ["filters[0][op]"]     = "in",
            ["filters[0][val]"]    = "1,3,5"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>()
                 .Map(d => d.ProductId, e => e.Id))
            .ToPagedListAsync();

        result.TotalEntries.ShouldBe(3);
        result.Select(p => p.Id).ShouldBe(new[] { 1, 3, 5 }, ignoreOrder: true);
    }

    // ── In – single value ────────────────────────────────────────────────────

    [Test]
    public async Task In_SingleValue_WorksLikeEquals() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"]     = "in",
            ["filters[0][val]"]    = "Accessories"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance))
            .ToPagedListAsync();

        result.TotalEntries.ShouldBe(2);
        result.ShouldAllBe(p => p.Category == "Accessories");
    }

    // ── In combined with other filters ───────────────────────────────────────

    [Test]
    public async Task In_CombinedWithOtherFilters_AppliesAndLogic() {
        // Category IN (Electronics, Books) AND InStock = true
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"]     = "in",
            ["filters[0][val]"]    = "Electronics,Books",
            ["filters[1][fields]"] = "InStock",
            ["filters[1][op]"]     = "eq",
            ["filters[1][val]"]    = "true"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance)
                 .Map(d => d.InStock, e => e.InStock))
            .ToPagedListAsync();

        // Electronics in stock: iPhone, Samsung; Books in stock: Clean Code, Kotlin in Action
        result.TotalEntries.ShouldBe(4);
        result.ShouldAllBe(p =>
            (p.Category == "Electronics" || p.Category == "Books") && p.InStock);
    }

    // ── In – no matches ──────────────────────────────────────────────────────

    [Test]
    public async Task In_NoMatchingValues_ReturnsEmpty() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"]     = "in",
            ["filters[0][val]"]    = "Furniture,Clothing"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance))
            .ToPagedListAsync();

        result.TotalEntries.ShouldBe(0);
        result.Count.ShouldBe(0);
    }
}
