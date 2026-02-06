# Pafiso
A .NET library for Paging, Filtering, and Sorting with DTO-to-Entity mapping support.

[![NuGet Version](https://img.shields.io/nuget/v/Pafiso.svg)](https://www.nuget.org/packages/Pafiso/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Pafiso.svg)](https://www.nuget.org/packages/Pafiso/)
![Build Status](https://img.shields.io/github/actions/workflow/status/fuji97/pafiso/test.yml)
![Deploy Status](https://img.shields.io/github/actions/workflow/status/fuji97/pafiso/deploy-package.yml)

Pafiso enables building dynamic, type-safe queries from query string parameters by mapping between DTOs (mapping models) and entity classes. Perfect for building flexible REST APIs with filtering, sorting, and pagination.

## Features

✨ **Fluent API** - Clean, discoverable syntax with IntelliSense support
🔒 **Type-Safe** - Strong typing with DTO-to-Entity mapping
🚀 **Entity Framework Core** - Full async support with optimized SQL
⚡ **Flexible** - Multiple API styles to fit your use case
🎯 **Auto-Mapping** - 1:1 field mapping when names match
🔧 **Customizable** - Transform values, map nested properties

## Installation

Install Pafiso via NuGet Package Manager:
```bash
PM> Install-Package Pafiso.AspNetCore
PM> Install-Package Pafiso.EntityFrameworkCore  # For EF Core async support
```

Or via the .NET CLI:
```bash
dotnet add package Pafiso.AspNetCore
dotnet add package Pafiso.EntityFrameworkCore  # For EF Core async support
```

## Quick Start

### 1. Define Your DTO and Entity

```csharp
using Pafiso;

// DTO - Represents incoming query parameters
public class ProductFilterDto : MappingModel {
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public string Category { get; set; }
}

// Entity - Your database model
public class Product {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public decimal Price { get; set; }
}
```

### 2. Use the Fluent API (Recommended)

```csharp
using Pafiso.EntityFrameworkCore;

[HttpGet]
public async Task<PagedList<Product>> GetProducts() {
    return await _dbContext.Products
        .WithPafiso(Request.Query, opt => {
            opt.WithPaging();
            opt.WithFiltering<ProductFilterDto>()
                .Map(dto => dto.ProductId, entity => entity.Id)
                .Map(dto => dto.ProductName, entity => entity.Name);
                // Category maps 1:1 automatically
            opt.WithSorting<ProductFilterDto>();
        })
        .ToPagedListAsync();
}
```

**That's it!** The query string is automatically parsed and applied.

### Query String Example

```
GET /api/products?skip=0&take=10
    &filters[0][fields]=Category&filters[0][op]=eq&filters[0][val]=Electronics
    &sortings[0][prop]=ProductName&sortings[0][ord]=asc
```

**Response:**
```json
{
  "totalEntries": 150,
  "pageNumber": 0,
  "pageSize": 10,
  "entries": [...]
}
```

## API Styles

Pafiso offers three API styles to fit different use cases:

### Style 1: Fluent Builder (Recommended)

Perfect for simple, one-time queries:

```csharp
var products = await _dbContext.Products
    .WithPafiso(Request.Query, opt => {
        opt.WithPaging();
        opt.WithFiltering<ProductFilterDto>();
    })
    .ToPagedListAsync();
```

### Style 2: SearchParameters (For Reusability)

Build once, reuse multiple times:

```csharp
// Build SearchParameters
var searchParams = Request.Query.ToSearchParameters<Product>(builder => {
    builder.WithPaging();
    builder.WithFiltering<ProductFilterDto>()
        .Map(dto => dto.ProductId, entity => entity.Id);
});

// Reuse across different queries
var activeProducts = await _dbContext.Products
    .Where(p => p.IsActive)
    .WithPafiso(searchParams)
    .ToPagedListAsync();

var featuredProducts = await _dbContext.Products
    .Where(p => p.IsFeatured)
    .WithPafiso(searchParams)
    .ToPagedListAsync();
```

### Style 3: Manual (Legacy)

For maximum control:

```csharp
var mapper = new FieldMapper<ProductFilterDto, Product>()
    .Map(dto => dto.ProductId, entity => entity.Id);

var searchParams = Request.Query.ToSearchParameters(mapper);
var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(_dbContext.Products);

var totalCount = await countQuery.CountAsync();
var items = await pagedQuery.ToListAsync();
```

## Core Concepts

### Automatic 1:1 Mapping

When DTO and Entity property names match, no explicit mapping is needed:

```csharp
public class ProductFilterDto : MappingModel {
    public string Category { get; set; }  // Matches Product.Category
}

// Category maps automatically
opt.WithFiltering<ProductFilterDto>();
```

### Custom Field Mapping

Map fields with different names:

```csharp
opt.WithFiltering<ProductFilterDto>()
    .Map(dto => dto.ProductId, entity => entity.Id)
    .Map(dto => dto.ProductName, entity => entity.Name);
```

### Value Transformation

Transform values before filtering:

```csharp
opt.WithFiltering<ProductFilterDto>()
    .MapWithTransform(
        dto => dto.PriceInDollars,
        entity => entity.PriceInCents,
        value => decimal.Parse(value ?? "0") * 100
    );
```

### Optional Components

All features are opt-in:

```csharp
.WithPafiso(Request.Query, opt => {
    opt.WithPaging();           // Optional
    opt.WithFiltering<Dto>();   // Optional
    opt.WithSorting<Dto>();     // Optional
});
```

## Supported Operators

### Filter Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `eq` | Equals | `filters[0][op]=eq&filters[0][val]=Electronics` |
| `neq` | Not equals | `filters[0][op]=neq&filters[0][val]=Books` |
| `gt` | Greater than | `filters[0][op]=gt&filters[0][val]=100` |
| `gte` | Greater than or equal | `filters[0][op]=gte&filters[0][val]=100` |
| `lt` | Less than | `filters[0][op]=lt&filters[0][val]=50` |
| `lte` | Less than or equal | `filters[0][op]=lte&filters[0][val]=50` |
| `contains` | String contains | `filters[0][op]=contains&filters[0][val]=laptop` |
| `startswith` | String starts with | `filters[0][op]=startswith&filters[0][val]=Pro` |
| `endswith` | String ends with | `filters[0][op]=endswith&filters[0][val]=Max` |

### Sort Orders

| Order | Description | Example |
|-------|-------------|---------|
| `asc` | Ascending | `sortings[0][ord]=asc` |
| `desc` | Descending | `sortings[0][ord]=desc` |

## Advanced Usage

### Custom Settings

Configure string comparison and other behaviors:

```csharp
var settings = new PafisoSettings {
    StringComparison = StringComparison.OrdinalIgnoreCase
};

.WithPafiso(Request.Query, settings, opt => {
    opt.WithFiltering<ProductFilterDto>();
});
```

### Nested Properties

Map to nested entity properties:

```csharp
public class Product {
    public Category Category { get; set; }
}

public class Category {
    public string Name { get; set; }
}

opt.WithFiltering<ProductFilterDto>()
    .Map(dto => dto.CategoryName, entity => entity.Category.Name);
```

### Multiple Filters (AND Logic)

Multiple filters are combined with AND:

```
?filters[0][fields]=Category&filters[0][op]=eq&filters[0][val]=Electronics
&filters[1][fields]=ProductId&filters[1][op]=gt&filters[1][val]=100
```

Result: `Category = 'Electronics' AND ProductId > 100`

### Multiple Fields (OR Logic)

Single filter with multiple fields uses OR:

```
?filters[0][fields]=Name,Description&filters[0][op]=contains&filters[0][val]=laptop
```

Result: `Name LIKE '%laptop%' OR Description LIKE '%laptop%'`

### Repository Pattern

```csharp
public interface IProductRepository {
    Task<PagedList<Product>> GetProductsAsync(SearchParameters searchParams);
}

public class ProductRepository : IProductRepository {
    private readonly AppDbContext _context;

    public async Task<PagedList<Product>> GetProductsAsync(SearchParameters searchParams) {
        return await _context.Products
            .WithPafiso(searchParams)
            .ToPagedListAsync();
    }
}
```

### Caching Search Preferences

```csharp
[HttpGet]
public async Task<PagedList<Product>> GetProducts([FromQuery] bool useLastSearch = false) {
    SearchParameters searchParams;

    if (useLastSearch) {
        searchParams = _cache.Get<SearchParameters>($"search_{userId}");
    } else {
        searchParams = Request.Query.ToSearchParameters<Product>(builder => {
            builder.WithPaging();
            builder.WithFiltering<ProductFilterDto>();
        });
        _cache.Set($"search_{userId}", searchParams, TimeSpan.FromHours(1));
    }

    return await _dbContext.Products
        .WithPafiso(searchParams)
        .ToPagedListAsync();
}
```

## Entity Framework Core Optimization

For optimized SQL generation with EF Core:

```csharp
using Pafiso.EntityFrameworkCore;

// In Program.cs
EfCoreExpressionBuilder.Register();
```

This enables `EF.Functions.Like` for case-insensitive string operations in SQL Server.

## DI Registration (Optional)

Register Pafiso services for dependency injection:

```csharp
using Pafiso.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register Pafiso with auto-detection of JSON settings
builder.Services.AddPafiso();

// Register custom settings
builder.Services.Configure<PafisoSettings>(options => {
    options.StringComparison = StringComparison.OrdinalIgnoreCase;
});
```

## Examples

See [EXAMPLES.md](EXAMPLES.md) for comprehensive usage examples including:
- Basic paging, filtering, sorting
- Custom field mappings
- Value transformations
- Repository pattern
- Conditional configurations
- Caching strategies

## Documentation

- **[DESIGN_DOCS.md](DESIGN_DOCS.md)** - Architecture and design decisions
- **[EXAMPLES.md](EXAMPLES.md)** - Comprehensive code examples
- **[NEW_FEATURES.md](NEW_FEATURES.md)** - SearchParameters API guide
- **[MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)** - Upgrading from older versions

## Why Pafiso?

### Before Pafiso

```csharp
[HttpGet]
public async Task<IActionResult> GetProducts(
    [FromQuery] string? category,
    [FromQuery] decimal? minPrice,
    [FromQuery] int page = 0,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? sortBy = "name",
    [FromQuery] string? sortOrder = "asc") {

    var query = _dbContext.Products.AsQueryable();

    if (!string.IsNullOrEmpty(category))
        query = query.Where(p => p.Category == category);

    if (minPrice.HasValue)
        query = query.Where(p => p.Price >= minPrice.Value);

    query = sortBy?.ToLower() switch {
        "name" => sortOrder == "desc"
            ? query.OrderByDescending(p => p.Name)
            : query.OrderBy(p => p.Name),
        "price" => sortOrder == "desc"
            ? query.OrderByDescending(p => p.Price)
            : query.OrderBy(p => p.Price),
        _ => query.OrderBy(p => p.Name)
    };

    var total = await query.CountAsync();
    var items = await query.Skip(page * pageSize).Take(pageSize).ToListAsync();

    return Ok(new { total, items });
}
```

### After Pafiso

```csharp
[HttpGet]
public async Task<PagedList<Product>> GetProducts() {
    return await _dbContext.Products
        .WithPafiso(Request.Query, opt => {
            opt.WithPaging();
            opt.WithFiltering<ProductFilterDto>();
            opt.WithSorting<ProductSortDto>();
        })
        .ToPagedListAsync();
}
```

**Benefits:**
- ✅ 90% less boilerplate code
- ✅ Type-safe with compile-time checking
- ✅ Flexible query strings without code changes
- ✅ Automatic parameter validation
- ✅ Consistent API across endpoints
- ✅ Testable and maintainable

## Performance

Pafiso generates optimized LINQ expressions that translate to efficient SQL:

```sql
-- Generated SQL (with EF Core)
SELECT COUNT(*) FROM Products WHERE Category = 'Electronics';

SELECT * FROM Products
WHERE Category = 'Electronics'
ORDER BY Name ASC
OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY;
```

No reflection in hot paths, no dynamic SQL, just clean LINQ-to-SQL.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

- 📖 [Documentation](https://github.com/fuji97/pafiso)
- 🐛 [Issue Tracker](https://github.com/fuji97/pafiso/issues)
- 💬 [Discussions](https://github.com/fuji97/pafiso/discussions)
