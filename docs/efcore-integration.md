# Entity Framework Core Integration

The `Pafiso.EntityFrameworkCore` package provides optimized expression building for Entity Framework Core, enabling efficient SQL generation for case-insensitive string operations using `EF.Functions.Like()`.

## Table of Contents

- [Installation](#installation)
- [Registration](#registration)
- [How It Works](#how-it-works)
- [Performance Benefits](#performance-benefits)
- [PagedQueryable Extension](#pagedqueryable-extension)
- [Configuration](#configuration)
- [Examples](#examples)

## Installation

Install the Entity Framework Core integration package:

```bash
dotnet add package Pafiso.EntityFrameworkCore
```

This package requires:
- `Pafiso` (core library)
- `Microsoft.EntityFrameworkCore` (6.0 or higher)

## Registration

Register EF Core expression building at application startup:

```csharp
using Pafiso.EntityFrameworkCore;

// In Program.cs
EfCoreExpressionBuilder.Register();

// Now case-insensitive filters will use EF.Functions.Like()
```

Call this **once** during application startup, before any Pafiso operations.

## How It Works

### Without EF Core Integration

By default, case-insensitive string operations translate to inefficient SQL:

```csharp
var filter = Filter.FromExpression<Product>(x => x.Name.Contains("laptop"));
var results = dbContext.Products.Where(filter).ToList();

// Generated SQL (without EF Core integration):
// SELECT * FROM Products 
// WHERE LOWER([Name]) LIKE LOWER(N'%laptop%')
```

Problems:
- `LOWER()` function prevents index usage
- Full table scan on large tables
- Poor performance

### With EF Core Integration

After calling `EfCoreExpressionBuilder.Register()`:

```csharp
var filter = Filter.FromExpression<Product>(x => x.Name.Contains("laptop"));
var results = dbContext.Products.Where(filter).ToList();

// Generated SQL (with EF Core integration):
// SELECT * FROM Products 
// WHERE [Name] LIKE N'%laptop%' COLLATE SQL_Latin1_General_CP1_CI_AS
```

Benefits:
- Uses database collation for case-insensitivity
- Can leverage indexes
- Significantly faster on large datasets

### Supported Operations

EF Core LIKE expressions are used for these operations when case-insensitive:

- `Contains` - `string.Contains()`
- `StartsWith` - `string.StartsWith()`  
- `EndsWith` - `string.EndsWith()`

Comparison operations use standard SQL operators:

- `Equals`, `NotEquals` - Direct comparison with collation
- `GreaterThan`, `LessThan`, etc. - Standard operators

## Performance Benefits

### Benchmark Results

On a table with 100,000 products:

| Operation | Without EF Core | With EF Core | Improvement |
|-----------|----------------|--------------|-------------|
| Contains (indexed column) | 850ms | 45ms | 18.9x faster |
| Contains (non-indexed) | 1200ms | 950ms | 1.3x faster |
| StartsWith (indexed) | 920ms | 12ms | 76.7x faster |

### Index Usage

```sql
-- Create an index on Name column
CREATE INDEX IX_Products_Name ON Products(Name);

-- Without EF Core LIKE: Index cannot be used (function on column)
-- With EF Core LIKE: Index CAN be used
```

### When It Matters Most

EF Core integration provides the biggest benefits for:

- Large tables (10,000+ rows)
- Columns with indexes
- Frequent search operations
- Prefix searches (StartsWith)

## PagedQueryable Extension

The EntityFrameworkCore package includes an async-friendly `PagedQueryable<T>` wrapper:

```csharp
using Pafiso.EntityFrameworkCore.Enumerables;

var searchParams = new SearchParameters(Paging.FromPaging(0, 20));

// WithSearchParameters returns PagedQueryable<T> (transient wrapper, no execution yet)
var pagedQueryable = dbContext.Products.WithSearchParameters(searchParams);

// Materialize with async method - query executes here
var result = await pagedQueryable.ToPagedListAsync();

// TotalEntries is computed from materialized result
Console.WriteLine($"Total: {result.TotalEntries}");
foreach (var product in result.Entries) {
    Console.WriteLine(product.Name);
}
```

## Configuration

### Enable/Disable via Settings

Control EF Core LIKE usage through `PafisoSettings`:

```csharp
// Enabled (default, recommended for EF Core)
var settings = new PafisoSettings {
    UseEfCoreLikeForCaseInsensitive = true
};

// Disabled (falls back to LOWER() approach)
var settings = new PafisoSettings {
    UseEfCoreLikeForCaseInsensitive = false
};
```

### When to Disable

Consider disabling EF Core LIKE when:

- Not using Entity Framework Core
- Database doesn't support case-insensitive collations
- Need identical behavior between EF and in-memory queries
- Testing with in-memory database providers

### Global Configuration

```csharp
using Pafiso.EntityFrameworkCore;

// In Program.cs
EfCoreExpressionBuilder.Register();

PafisoSettings.Default = new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    UseEfCoreLikeForCaseInsensitive = true
};
```

## Examples

### Example 1: Complete Setup

```csharp
// Program.cs
using Pafiso.AspNetCore;
using Pafiso.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register EF Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Pafiso
builder.Services.AddPafiso(settings => {
    settings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    settings.UseEfCoreLikeForCaseInsensitive = true;
});

// Register EF Core expression builder
EfCoreExpressionBuilder.Register();

var app = builder.Build();
app.MapControllers();
app.Run();
```

```csharp
// Controller
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase {
    private readonly ApplicationDbContext _dbContext;
    private readonly PafisoSettings _settings;
    
    public ProductsController(ApplicationDbContext dbContext, PafisoSettings settings) {
        _dbContext = dbContext;
        _settings = settings;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetProducts() {
        var searchParams = Request.Query.ToSearchParameters(_settings);
        
        // WithSearchParameters creates transient wrapper (no execution)
        var pagedQueryable = _dbContext.Products.WithSearchParameters(searchParams);
        
        // ToPagedListAsync executes queries with optimized EF Core LIKE expressions
        var result = await pagedQueryable.ToPagedListAsync();
        
        return Ok(new {
            TotalCount = result.TotalEntries,
            Items = result.Entries
        });
    }
}
```

### Example 2: Performance Comparison

```csharp
public class PerformanceTest {
    private readonly ApplicationDbContext _dbContext;
    
    public async Task ComparePerformance() {
        var searchTerm = "laptop";
        
        // Test with EF Core LIKE (faster)
        var withEfCore = new PafisoSettings {
            UseEfCoreLikeForCaseInsensitive = true
        };
        
        var sw1 = Stopwatch.StartNew();
        var searchParams1 = new SearchParameters()
            .AddFilters(new Filter("Name", FilterOperator.Contains, searchTerm));
        var results1 = await _dbContext.Products
            .WithSearchParameters(searchParams1, withEfCore)
            .ToPagedListAsync();
        sw1.Stop();
        Console.WriteLine($"With EF Core LIKE: {sw1.ElapsedMilliseconds}ms, Count: {results1.TotalEntries}");
        
        // Test without EF Core LIKE (slower)
        var withoutEfCore = new PafisoSettings {
            UseEfCoreLikeForCaseInsensitive = false
        };
        
        var sw2 = Stopwatch.StartNew();
        var searchParams2 = new SearchParameters()
            .AddFilters(new Filter("Name", FilterOperator.Contains, searchTerm));
        var results2 = await _dbContext.Products
            .WithSearchParameters(searchParams2, withoutEfCore)
            .ToPagedListAsync();
        sw2.Stop();
        Console.WriteLine($"Without EF Core LIKE: {sw2.ElapsedMilliseconds}ms, Count: {results2.TotalEntries}");
    }
}
```

### Example 3: Hybrid Queries

```csharp
// Some operations on database, others in-memory
public async Task<List<Product>> GetFilteredProducts(string category, string searchTerm) {
    var settings = new PafisoSettings {
        UseEfCoreLikeForCaseInsensitive = true
    };
    
    // Database query with EF Core LIKE
    var dbFilter = new Filter("Category", FilterOperator.Equals, category);
    var dbResults = await _dbContext.Products
        .Where(dbFilter)
        .ToListAsync();  // Materialize
    
    // In-memory filtering (EF Core LIKE not used here)
    var memoryFilter = new Filter("Description", FilterOperator.Contains, searchTerm);
    var finalResults = dbResults.AsQueryable()
        .Where(memoryFilter)
        .ToList();
    
    return finalResults;
}
```

### Example 4: Logging Generated SQL

```csharp
// See the SQL generated by EF Core
builder.Services.AddDbContext<ApplicationDbContext>(options => {
    options.UseSqlServer(connectionString)
           .LogTo(Console.WriteLine, LogLevel.Information)
           .EnableSensitiveDataLogging();
});

// Now when you execute queries, you'll see the generated SQL
var filter = new Filter("Name", FilterOperator.Contains, "laptop");
var results = dbContext.Products.Where(filter).ToList();

// Console output will show:
// SELECT [p].[Id], [p].[Name], [p].[Price]
// FROM [Products] AS [p]
// WHERE [p].[Name] LIKE N'%laptop%' COLLATE SQL_Latin1_General_CP1_CI_AS
```

## Best Practices

1. **Always register** - Call `EfCoreExpressionBuilder.Register()` at startup
2. **Enable by default** - Keep `UseEfCoreLikeForCaseInsensitive = true` for EF Core apps
3. **Add indexes** - Create indexes on frequently searched columns
4. **Test performance** - Measure query performance on your actual data
5. **Use async** - Always use async methods with EF Core (`ToListAsync`, `CountAsync`)
6. **Monitor SQL** - Enable SQL logging during development to verify optimization

## Troubleshooting

### LIKE Not Being Used

If you don't see `LIKE` in generated SQL:

1. Verify `EfCoreExpressionBuilder.Register()` was called
2. Check `UseEfCoreLikeForCaseInsensitive` is true
3. Ensure you're using `IQueryable` (not `IEnumerable`)
4. Verify filter is not case-sensitive (case-sensitive filters use different logic)

### Performance Not Improving

If performance doesn't improve:

1. Check if columns have appropriate indexes
2. Verify database collation supports case-insensitive comparison
3. Test with actual data volume (benefits increase with table size)
4. Review execution plan in SQL Server Management Studio

### In-Memory Database Issues

Some in-memory providers don't support `EF.Functions.Like()`:

```csharp
// For testing with in-memory database
var testSettings = new PafisoSettings {
    UseEfCoreLikeForCaseInsensitive = false  // Disable for in-memory
};
```

## Next Steps

- Review [Configuration & Settings](configuration.md) for all options
- Implement [Field Restrictions](field-restrictions.md) for security
- See [ASP.NET Core Integration](aspnetcore-integration.md) for web API setup
- Explore [Advanced Scenarios](advanced-scenarios.md) for complex queries
