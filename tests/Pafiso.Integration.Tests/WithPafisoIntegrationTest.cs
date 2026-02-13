using System;
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

/// <summary>
/// Integration tests for <see cref="QueryableExtensions.WithPafiso{T}"/> using SQLite.
/// Each test builds a query string via <see cref="SearchParameters.ToDictionary"/> (or manually),
/// feeds it through <c>WithPafiso</c>, and verifies the results against a real SQLite database.
/// </summary>
public class WithPafisoIntegrationTest {
    // ── DTOs ──────────────────────────────────────────────────────────────────

    public class ProductFilterDto : MappingModel {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Category { get; set; }
        public decimal Price { get; set; }
        public bool InStock { get; set; }
    }

    public class ProductSortDto : MappingModel {
        public string? ProductName { get; set; }
        public string? Category { get; set; }
        public decimal Price { get; set; }
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IQueryCollection ToQueryCollection(IDictionary<string, string> dict) {
        return new QueryCollection(
            dict.ToDictionary(kvp => kvp.Key, kvp => new StringValues(kvp.Value)));
    }

    private static IQueryCollection ToQueryCollection(SearchParameters sp) =>
        ToQueryCollection(sp.ToDictionary());

    // ── No-configuration (pass-through) ──────────────────────────────────────

    [Test]
    public async Task WithPafiso_NoConfigure_ReturnsAllItems() {
        var qc = ToQueryCollection(new Dictionary<string, string>());

        var result = await _context.Products
            .WithPafiso(qc)
            .ToPagedListAsync();

        result.TotalEntries.ShouldBe(8);
        result.Count.ShouldBe(8);
    }

    // ── Paging ────────────────────────────────────────────────────────────────

    [Test]
    public async Task WithPafiso_Paging_FirstPage_ReturnsCorrectSlice() {
        // Build paging via Pafiso API then serialize to query string
        var paging = Paging.FromSkipTake(0, 3);
        var sp = new SearchParameters { Paging = paging };
        var qc = ToQueryCollection(sp);

        var result = await _context.Products
            .WithPafiso(qc, configure: b => b.WithPaging())
            .ToPagedListAsync();

        result.TotalEntries.ShouldBe(8);
        result.Count.ShouldBe(3);
    }

    [Test]
    public async Task WithPafiso_Paging_SecondPage_ReturnsCorrectSlice() {
        var paging = Paging.FromSkipTake(3, 3);
        var sp = new SearchParameters { Paging = paging };
        var qc = ToQueryCollection(sp);

        var result = await _context.Products
            .WithPafiso(qc, configure: b => b.WithPaging())
            .ToPagedListAsync();

        result.TotalEntries.ShouldBe(8);
        result.Count.ShouldBe(3);
    }

    [Test]
    public async Task WithPafiso_Paging_LastPartialPage_ReturnsRemainingItems() {
        var paging = Paging.FromSkipTake(6, 5); // only 2 items remain
        var sp = new SearchParameters { Paging = paging };
        var qc = ToQueryCollection(sp);

        var result = await _context.Products
            .WithPafiso(qc, configure: b => b.WithPaging())
            .ToPagedListAsync();

        result.TotalEntries.ShouldBe(8);
        result.Count.ShouldBe(2);
    }

    // ── Filtering – Equals ────────────────────────────────────────────────────

    [Test]
    public async Task WithPafiso_FilterEquals_Category_ReturnsMatchingItems() {
        // Build SearchParameters with a filter then round-trip through the query string
        var mapper = new FieldMapper<ProductFilterDto, Product>()
            .Map(d => d.Category, e => e.Category);

        var filter = Filter.WithMapper("Category", FilterOperator.Equals, "Books", mapper,
            EfCoreExpressionBuilder.Instance);
        var sp = new SearchParameters { Filters = [filter] };
        var qc = ToQueryCollection(sp);

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance))
            .ToPagedListAsync();

        result.TotalEntries.ShouldBe(3);
        result.ShouldAllBe(p => p.Category == "Books");
    }

    [Test]
    public async Task WithPafiso_FilterEquals_MappedField_FiltersById() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "ProductId",
            ["filters[0][op]"]     = "eq",
            ["filters[0][val]"]    = "3"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>()
                 .Map(d => d.ProductId, e => e.Id))
            .ToPagedListAsync();

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(3);
    }

    // ── Filtering – Contains ──────────────────────────────────────────────────

    [Test]
    public async Task WithPafiso_FilterContains_ProductName_ReturnsMatches() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "ProductName",
            ["filters[0][op]"]     = "contains",
            ["filters[0][val]"]    = "Phone"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance)
                 .Map(d => d.ProductName, e => e.Name))
            .ToPagedListAsync();

        // "Apple iPhone" and "Sony Headphones" both contain "phone"
        result.Count.ShouldBe(2);
        result.ShouldAllBe(p => p.Name.Contains("Phone", StringComparison.OrdinalIgnoreCase));
    }

    // ── Filtering – GreaterThan / LessThan ────────────────────────────────────

    [Test]
    public async Task WithPafiso_FilterGreaterThan_Price_ReturnsExpensiveItems() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Price",
            ["filters[0][op]"]     = "gt",
            ["filters[0][val]"]    = "200"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>()
                 .Map(d => d.Price, e => e.Price))
            .ToPagedListAsync();

        result.ShouldAllBe(p => p.Price > 200m);
        result.Count.ShouldBe(3); // iPhone(999), Samsung(799), Sony(299)
    }

    [Test]
    public async Task WithPafiso_FilterLessThan_Price_ReturnsCheapItems() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Price",
            ["filters[0][op]"]     = "lt",
            ["filters[0][val]"]    = "100"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>()
                 .Map(d => d.Price, e => e.Price))
            .ToPagedListAsync();

        result.ShouldAllBe(p => p.Price < 100m);
        result.Count.ShouldBe(4); // Clean Code(35), Pragmatic(45), Kotlin(55), Logitech(79)
    }

    // ── Filtering – StartsWith / EndsWith ─────────────────────────────────────

    [Test]
    public async Task WithPafiso_FilterStartsWith_ProductName_ReturnsMatches() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "ProductName",
            ["filters[0][op]"]     = "startswith",
            ["filters[0][val]"]    = "Apple"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance)
                 .Map(d => d.ProductName, e => e.Name))
            .ToPagedListAsync();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Apple iPhone");
    }

    [Test]
    public async Task WithPafiso_FilterEndsWith_ProductName_ReturnsMatches() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "ProductName",
            ["filters[0][op]"]     = "endswith",
            ["filters[0][val]"]    = "Mouse"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance)
                 .Map(d => d.ProductName, e => e.Name))
            .ToPagedListAsync();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Logitech Mouse");
    }

    // ── Filtering – Null / NotNull ────────────────────────────────────────────

    [Test]
    public async Task WithPafiso_FilterBoolean_InStock_ReturnsOnlyInStockItems() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "InStock",
            ["filters[0][op]"]     = "eq",
            ["filters[0][val]"]    = "true"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>()
                 .Map(d => d.InStock, e => e.InStock))
            .ToPagedListAsync();

        result.ShouldAllBe(p => p.InStock);
        result.Count.ShouldBe(5); // iPhone, Samsung, Clean Code, Kotlin, Logitech
    }

    // ── Multiple filters ──────────────────────────────────────────────────────

    [Test]
    public async Task WithPafiso_MultipleFilters_CombinesConditions() {
        // Electronics AND Price < 500
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"]     = "eq",
            ["filters[0][val]"]    = "Electronics",
            ["filters[1][fields]"] = "Price",
            ["filters[1][op]"]     = "lt",
            ["filters[1][val]"]    = "500"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance)
                 .Map(d => d.Price, e => e.Price))
            .ToPagedListAsync();

        result.ShouldAllBe(p => p.Category == "Electronics" && p.Price < 500m);
        result.Count.ShouldBe(1); // Sony Headphones (299)
    }

    // ── Sorting ───────────────────────────────────────────────────────────────

    [Test]
    public async Task WithPafiso_SortByPrice_Ascending_ReturnsCorrectOrder() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["sortings[0][prop]"] = "Price",
            ["sortings[0][ord]"]  = "asc"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithSorting<ProductSortDto>()
                 .Map(d => d.Price, e => e.Price))
            .ToPagedListAsync();

        result.Count.ShouldBe(8);
        for (int i = 0; i < result.Count - 1; i++)
            result[i].Price.ShouldBeLessThanOrEqualTo(result[i + 1].Price);
    }

    [Test]
    public async Task WithPafiso_SortByPrice_Descending_ReturnsCorrectOrder() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["sortings[0][prop]"] = "Price",
            ["sortings[0][ord]"]  = "desc"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithSorting<ProductSortDto>()
                 .Map(d => d.Price, e => e.Price))
            .ToPagedListAsync();

        result.Count.ShouldBe(8);
        result[0].Price.ShouldBe(999m);
        result[7].Price.ShouldBe(35m);
    }

    [Test]
    public async Task WithPafiso_SortByCategory_ThenByPrice_Ascending() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["sortings[0][prop]"] = "Category",
            ["sortings[0][ord]"]  = "asc",
            ["sortings[1][prop]"] = "Price",
            ["sortings[1][ord]"]  = "asc"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b => {
                b.WithSorting<ProductSortDto>()
                 .Map(d => d.Category, e => e.Category)
                 .Map(d => d.Price, e => e.Price);
            })
            .ToPagedListAsync();

        // Categories sorted: Accessories, Books, Electronics
        result[0].Category.ShouldBe("Accessories");
        result[1].Category.ShouldBe("Accessories");
        result[2].Category.ShouldBe("Books");
    }

    // ── Filter + Sort + Page combined ─────────────────────────────────────────

    [Test]
    public async Task WithPafiso_FilterSortPage_Combined_ReturnsCorrectResults() {
        // Filter: Category = Electronics, Sort: Price desc, Page: skip=0 take=2
        var paging   = Paging.FromSkipTake(0, 2);
        var sp       = new SearchParameters { Paging = paging };
        var baseDict = sp.ToDictionary();

        var dict = new Dictionary<string, string>(baseDict) {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"]     = "eq",
            ["filters[0][val]"]    = "Electronics",
            ["sortings[0][prop]"]  = "Price",
            ["sortings[0][ord]"]   = "desc"
        };

        var qc = ToQueryCollection(dict);

        var result = await _context.Products
            .WithPafiso(qc, configure: b => {
                b.WithPaging();
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance);
                b.WithSorting<ProductSortDto>()
                 .Map(d => d.Price, e => e.Price);
            })
            .ToPagedListAsync();

        result.TotalEntries.ShouldBe(3); // 3 Electronics items
        result.Count.ShouldBe(2);        // but only 2 per page
        result[0].Price.ShouldBe(999m);  // iPhone
        result[1].Price.ShouldBe(799m);  // Samsung
        result[0].Category.ShouldBe("Electronics");
    }

    // ── Round-trip query string serialization ─────────────────────────────────

    [Test]
    public async Task WithPafiso_QueryStringRoundTrip_ProducesConsistentResults() {
        // Create SearchParameters programmatically, serialize to query string, then
        // feed through WithPafiso — verifying the full round-trip serialization path.
        var mapper = new FieldMapper<ProductFilterDto, Product>()
            .Map(d => d.Category, e => e.Category);

        // Build filter with EfCoreExpressionBuilder so it is translatable by SQLite
        var filter  = Filter.WithMapper("Category", FilterOperator.Equals, "Books", mapper,
            EfCoreExpressionBuilder.Instance);
        var sorting = Sorting.WithMapper("Price", SortOrder.Ascending, mapper);
        var paging  = Paging.FromSkipTake(0, 10);

        var sp = new SearchParameters {
            Filters  = [filter],
            Sortings = [sorting],
            Paging   = paging
        };

        // Serialize to query string then back
        var dict = sp.ToDictionary();
        var qc   = ToQueryCollection(dict);

        var result = await _context.Products
            .WithPafiso(qc, configure: b => {
                b.WithPaging();
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance)
                 .Map(d => d.Category, e => e.Category);
                b.WithSorting<ProductSortDto>()
                 .Map(d => d.Price, e => e.Price);
            })
            .ToPagedListAsync();

        result.TotalEntries.ShouldBe(3);
        result.ShouldAllBe(p => p.Category == "Books");
        // Prices should be in ascending order
        result[0].Price.ShouldBeLessThanOrEqualTo(result[1].Price);
    }

    [Test]
    public async Task WithPafiso_SearchParametersOverload_ProducesSameResults() {
        // Build SearchParameters then pass them directly (second WithPafiso overload).
        // Use EfCoreExpressionBuilder so string comparisons are translated by SQLite.
        var mapper = new FieldMapper<ProductFilterDto, Product>()
            .Map(d => d.Category, e => e.Category);

        var filter  = Filter.WithMapper("Category", FilterOperator.Equals, "Accessories", mapper,
            EfCoreExpressionBuilder.Instance);
        var sorting = Sorting.WithMapper("Price", SortOrder.Descending, mapper);
        var paging  = Paging.FromSkipTake(0, 10);

        var sp = new SearchParameters {
            Filters  = [filter],
            Sortings = [sorting],
            Paging   = paging
        };

        var result = await _context.Products
            .WithPafiso(sp)
            .ToPagedListAsync();

        result.TotalEntries.ShouldBe(2);
        result.ShouldAllBe(p => p.Category == "Accessories");
        result[0].Price.ShouldBeGreaterThan(result[1].Price);
    }

    // ── EfCoreExpressionBuilder (LIKE-based) ──────────────────────────────────

    [Test]
    public async Task WithPafiso_EfBuilder_CaseInsensitiveContains_MatchesAllCases() {
        // SQLite default collation is case-insensitive for ASCII; this verifies LIKE integration
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "ProductName",
            ["filters[0][op]"]     = "contains",
            ["filters[0][val]"]    = "iphone",
            ["filters[0][case]"]   = "false"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance)
                 .Map(d => d.ProductName, e => e.Name))
            .ToPagedListAsync();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Apple iPhone");
    }

    [Test]
    public async Task WithPafiso_EfBuilder_StartsWith_MatchesPrefix() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "ProductName",
            ["filters[0][op]"]     = "startswith",
            ["filters[0][val]"]    = "Samsung"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance)
                 .Map(d => d.ProductName, e => e.Name))
            .ToPagedListAsync();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Samsung Galaxy");
    }

    [Test]
    public async Task WithPafiso_EfBuilder_EndsWith_MatchesSuffix() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "ProductName",
            ["filters[0][op]"]     = "endswith",
            ["filters[0][val]"]    = "Action"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance)
                 .Map(d => d.ProductName, e => e.Name))
            .ToPagedListAsync();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Kotlin in Action");
    }

    // ── Contains – case-sensitive & case-insensitive ─────────────────────────

    [Test]
    public async Task WithPafiso_Contains_CaseSensitive_MatchesExactCase() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "ProductName",
            ["filters[0][op]"]     = "contains",
            ["filters[0][val]"]    = "Clean",
            ["filters[0][case]"]   = "true"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance)
                 .Map(d => d.ProductName, e => e.Name))
            .ToPagedListAsync();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Clean Code");
    }

    [Test]
    public async Task WithPafiso_Contains_CaseInsensitive_MatchesRegardlessOfCase() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "ProductName",
            ["filters[0][op]"]     = "contains",
            ["filters[0][val]"]    = "galaxy",
            ["filters[0][case]"]   = "false"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance)
                 .Map(d => d.ProductName, e => e.Name))
            .ToPagedListAsync();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Samsung Galaxy");
    }

    [Test]
    public async Task WithPafiso_Contains_CaseInsensitive_MatchesMultipleItems() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "ProductName",
            ["filters[0][op]"]     = "contains",
            ["filters[0][val]"]    = "CODE",
            ["filters[0][case]"]   = "false"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance)
                 .Map(d => d.ProductName, e => e.Name))
            .ToPagedListAsync();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Clean Code");
    }

    // ── StartsWith – case-sensitive & case-insensitive ─────────────────────

    [Test]
    public async Task WithPafiso_StartsWith_CaseSensitive_MatchesExactCase() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "ProductName",
            ["filters[0][op]"]     = "startswith",
            ["filters[0][val]"]    = "Apple",
            ["filters[0][case]"]   = "true"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance)
                 .Map(d => d.ProductName, e => e.Name))
            .ToPagedListAsync();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Apple iPhone");
    }

    [Test]
    public async Task WithPafiso_StartsWith_CaseInsensitive_MatchesRegardlessOfCase() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "ProductName",
            ["filters[0][op]"]     = "startswith",
            ["filters[0][val]"]    = "samsung",
            ["filters[0][case]"]   = "false"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance)
                 .Map(d => d.ProductName, e => e.Name))
            .ToPagedListAsync();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Samsung Galaxy");
    }

    [Test]
    public async Task WithPafiso_StartsWith_CaseInsensitive_MatchesMultipleItems() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"]     = "startswith",
            ["filters[0][val]"]    = "ELEC",
            ["filters[0][case]"]   = "false"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance))
            .ToPagedListAsync();

        result.Count.ShouldBe(3);
        result.ShouldAllBe(p => p.Category == "Electronics");
    }

    // ── EndsWith – case-sensitive & case-insensitive ───────────────────────

    [Test]
    public async Task WithPafiso_EndsWith_CaseSensitive_MatchesExactCase() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "ProductName",
            ["filters[0][op]"]     = "endswith",
            ["filters[0][val]"]    = "Mouse",
            ["filters[0][case]"]   = "true"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance)
                 .Map(d => d.ProductName, e => e.Name))
            .ToPagedListAsync();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Logitech Mouse");
    }

    [Test]
    public async Task WithPafiso_EndsWith_CaseInsensitive_MatchesRegardlessOfCase() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "ProductName",
            ["filters[0][op]"]     = "endswith",
            ["filters[0][val]"]    = "ACTION",
            ["filters[0][case]"]   = "false"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance)
                 .Map(d => d.ProductName, e => e.Name))
            .ToPagedListAsync();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Kotlin in Action");
    }

    [Test]
    public async Task WithPafiso_EndsWith_CaseInsensitive_MatchesMultipleItems() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"]     = "endswith",
            ["filters[0][val]"]    = "ICS",
            ["filters[0][case]"]   = "false"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance))
            .ToPagedListAsync();

        result.Count.ShouldBe(3);
        result.ShouldAllBe(p => p.Category == "Electronics");
    }

    // ── Edge cases ────────────────────────────────────────────────────────────

    [Test]
    public async Task WithPafiso_FilterThatMatchesNothing_ReturnsEmptyList() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"]     = "eq",
            ["filters[0][val]"]    = "NonExistentCategory"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance))
            .ToPagedListAsync();

        result.Count.ShouldBe(0);
        result.TotalEntries.ShouldBe(0);
    }

    [Test]
    public async Task WithPafiso_PagingBeyondData_ReturnsEmptyEntriesWithCorrectTotal() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["skip"] = "100",
            ["take"] = "10"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b => b.WithPaging())
            .ToPagedListAsync();

        result.TotalEntries.ShouldBe(8);
        result.Count.ShouldBe(0);
    }

    [Test]
    public async Task WithPafiso_FilterNotEquals_ExcludesMatchingItems() {
        var qc = ToQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"]     = "neq",
            ["filters[0][val]"]    = "Electronics"
        });

        var result = await _context.Products
            .WithPafiso(qc, configure: b =>
                b.WithFiltering<ProductFilterDto>(EfCoreExpressionBuilder.Instance))
            .ToPagedListAsync();

        result.ShouldAllBe(p => p.Category != "Electronics");
        result.Count.ShouldBe(5); // Books(3) + Accessories(2)
    }
}
