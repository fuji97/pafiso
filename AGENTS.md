# AGENTS.md

This file provides guidance to AI agents when working with code in this repository.

## Project Overview

Pafiso is a .NET 10 library for serializing, deserializing, and applying Paging, Filtering, and Sorting to `IQueryable<T>` and `IEnumerable<T>` collections. It supports mapping between DTOs (mapping models) and entity classes, enabling building dynamic queries from query string parameters with flexible field mappings.

## Build and Test Commands

```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run a specific test
dotnet test --filter "FullyQualifiedName~FilterTest.Equals"

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Architecture

### Core Types

- **`SearchParameters`** - Combines Paging, Sorting, and Filter into a single object. **Primary method**: Apply to queries via `ApplyToIQueryable<T>()`, which returns a tuple of `(countQuery, pagedQuery)` for separate count and paged result execution. Supports field restrictions and mapper-based field resolution. Serializes to/from dictionary for query string support.

- **`Filter`** - Represents a filter condition with field(s), operator, value, and case sensitivity. Multiple fields create OR conditions. **Requires a mapper** - create using `Filter.WithMapper<TMapping, TEntity>(field, operator, value, mapper)` to map DTO fields to entity fields.

- **`Sorting`** - Represents sort order for a property. **Requires a mapper** - create using `Sorting.WithMapper<TMapping, TEntity>(propertyName, sortOrder, mapper)` to map DTO fields to entity fields.

- **`Paging`** - Represents pagination as skip/take. Create via `Paging.FromPaging(page, pageSize)` or `Paging.FromSkipTake(skip, take)`.

- **`MappingModel`** - Abstract base class for all mapping models (DTOs) used with the field mapper system. Provides lifecycle hooks (`OnBeforeMap()`, `OnAfterMap()`, `Validate()`).

- **`IFieldMapper<TMapping, TEntity>`** - Interface for mapping field names from DTOs to entity properties. Supports custom field mappings and value transformations.
  - `ResolveToEntityField(string)` - Resolves DTO field name to entity field name
  - `TransformValue<TProperty>(string, string?)` - Transforms raw string values to typed values
  - `GetMappedFields()` - Returns all valid field names from the mapping model

- **`FieldMapper<TMapping, TEntity>`** - Default implementation of `IFieldMapper`. Provides fluent API:
  - `Map(mappingField, entityField)` - Maps DTO field to entity field
  - `MapWithTransform<TValue>(mappingField, entityField, transformer)` - Maps with value transformation
  - `WithTransform<TValue>(mappingField, transformer)` - Registers value transformer for 1:1 mapped field

- **`PafisoSettings`** - Configuration for field name mapping, case sensitivity, and EF Core integration. Key properties:
  - `PropertyNamingPolicy` - Uses `System.Text.Json.JsonNamingPolicy` (CamelCase, SnakeCaseLower, etc.)
  - `UseJsonPropertyNameAttributes` - Respects `[JsonPropertyName]` attributes on properties
  - `StringComparison` - Configurable string comparison (default: `OrdinalIgnoreCase`)
  - `UseEfCoreLikeForCaseInsensitive` - Uses `EF.Functions.Like` for EF Core scenarios
  - `PafisoSettings.Default` - Static property for global configuration

- **`IFieldNameResolver`** - Interface for resolving filter/sorting field names to property names. Implementations:
  - `DefaultFieldNameResolver` - Uses JSON naming policy and `[JsonPropertyName]` attributes
  - `PassThroughFieldNameResolver` - Returns field names unchanged

### Result Types

- **`PagedList<T>`** - Materialized result containing `Entries` (List<T>) and `TotalEntries` (int).

### Key Dependencies

- **LinqKit** - Used for `PredicateBuilder` to compose OR predicates across multiple filter fields

## Additional Packages

### Pafiso.EntityFrameworkCore

Provides EF Core-specific expression building for optimized SQL translation:

- **`EfCoreExpressionBuilder`** - Registers `EF.Functions.Like` support for case-insensitive string operations
  - Call `EfCoreExpressionBuilder.Register()` at startup
  - Sets `ExpressionUtilities.EfCoreLikeExpressionBuilder` delegate

### Pafiso.AspNetCore

Provides ASP.NET Core integration:

- **`QueryCollectionExtensions`** - `ToSearchParameters<TMapping, TEntity>()` extension for `IQueryCollection`. Requires a mapper instance. Takes optional `PafisoSettings` parameter.
- **`ServiceCollectionExtensions`** - `AddPafiso()` for DI registration, auto-detects MVC JSON settings and registers `PafisoSettings` as singleton

## Recommended Usage Pattern

### ASP.NET Core Controllers with Mapping Models

```csharp
// Define your mapping model (DTO)
public class ProductSearchDto : MappingModel {
    public string? ProductName { get; set; }
    public decimal? MinPrice { get; set; }
    public string? Category { get; set; }
}

// Define your entity
public class Product {
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase {
    private readonly ApplicationDbContext _dbContext;
    private readonly PafisoSettings _settings;
    private readonly IFieldMapper<ProductSearchDto, Product> _mapper;

    public ProductsController(ApplicationDbContext dbContext, PafisoSettings settings) {
        _dbContext = dbContext;
        _settings = settings;

        // Configure mapper (can also be registered in DI)
        _mapper = new FieldMapper<ProductSearchDto, Product>(settings)
            .Map(dto => dto.ProductName, entity => entity.Name)
            .Map(dto => dto.MinPrice, entity => entity.Price);
            // Category maps 1:1, so no explicit mapping needed
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts() {
        // Parse query string with mapper
        var searchParams = Request.Query.ToSearchParameters<ProductSearchDto, Product>(_mapper, _settings);

        // ApplyToIQueryable returns (countQuery, pagedQuery)
        var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(_dbContext.Products);

        // Execute queries
        var totalCount = await countQuery.CountAsync();
        var items = await pagedQuery.ToListAsync();

        return Ok(new {
            TotalCount = totalCount,
            Items = items
        });
    }
}
```

Example query string:
```
GET /api/products?skip=0&take=10&filters[0][fields]=productName&filters[0][op]=contains&filters[0][val]=laptop&sortings[0][prop]=minPrice&sortings[0][ord]=asc
```

### Registering Mappers in DI

**Recommended approach: Register mappers as singletons**
```csharp
// In Program.cs
builder.Services.AddSingleton<IFieldMapper<ProductSearchDto, Product>>(sp => {
    var settings = sp.GetRequiredService<PafisoSettings>();
    return new FieldMapper<ProductSearchDto, Product>(settings)
        .Map(dto => dto.ProductName, entity => entity.Name)
        .Map(dto => dto.MinPrice, entity => entity.Price);
});

// In controller
public class ProductsController : ControllerBase {
    private readonly IFieldMapper<ProductSearchDto, Product> _mapper;

    public ProductsController(IFieldMapper<ProductSearchDto, Product> mapper) {
        _mapper = mapper;
    }
}
```

### Setup in Program.cs

```csharp
// Register Pafiso with auto-detection of JSON settings
builder.Services.AddPafiso();

// Or configure manually
builder.Services.AddPafiso(settings => {
    settings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    settings.UseEfCoreLikeForCaseInsensitive = true;
});

// Register EF Core optimizations (if using Pafiso.EntityFrameworkCore)
EfCoreExpressionBuilder.Register();
```

## Testing

Tests use NUnit 4 with Shouldly for assertions.

### Core Test Files
- `ExpressionTests.cs` - Tests for expression building and utilities

### Mapping Test Files (in `tests/Pafiso.Tests/Mapping/`)
- `FieldMapperTests.cs` - Tests for FieldMapper configuration and resolution
- `FilterWithMapperTests.cs` - Filter integration tests with mapper
- `SortingWithMapperTests.cs` - Sorting integration tests with mapper
- `SearchParametersWithMapperTests.cs` - SearchParameters integration tests with mapper

### Package-Specific Tests
- `EfCoreExpressionBuilderTest.cs` - EF Core expression builder tests (in Pafiso.EntityFrameworkCore.Tests)
- `PagedQueryableAsyncTest.cs` - EF Core async paging tests (in Pafiso.EntityFrameworkCore.Tests)
- `QueryCollectionExtensionsTest.cs` - ASP.NET Core query collection tests (in Pafiso.AspNetCore.Tests)
- `ServiceCollectionExtensionsTest.cs` - ASP.NET Core DI tests (in Pafiso.AspNetCore.Tests)
