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

- **`SearchParameters`** - Combines Paging, Sorting, and Filter into a single object. **Primary method**: Apply to queries via `ApplyToIQueryable<T>()`, which returns a tuple of `(countQuery, pagedQuery)` for separate count and paged result execution. Supports serialization via `ToDictionary()` and deserialization via `FromDictionary<TMapping, TEntity>()` and `FromJson<TMapping, TEntity>()`.

- **`Filter`** - Represents a filter condition with field(s), operator, value, and case sensitivity. Multiple fields create OR conditions. **Requires a mapper** - create using `Filter.WithMapper<TMapping, TEntity>(fields, operator, value, mapper)`. Optionally accepts an `IFilterExpressionBuilder` for custom expression building (e.g., EF Core's `EF.Functions.Like`).

- **`Sorting`** - Represents sort order for a property. **Requires a mapper** - create using `Sorting.WithMapper<TMapping, TEntity>(propertyName, sortOrder, mapper)`. Optionally accepts an `ISortingExpressionBuilder` for custom expression building.

- **`Paging`** - Represents pagination as skip/take. Create via `Paging.FromPaging(page, pageSize)` or `Paging.FromSkipTake(skip, take)`.

- **`MappingModel`** - Abstract base class for all mapping models (DTOs) used with the field mapper system. Provides lifecycle hooks (`OnBeforeMap()`, `OnAfterMap()`, `Validate()`).

- **`IFieldMapper<TMapping, TEntity>`** - Interface for mapping field names from DTOs to entity properties. Supports custom field mappings and value transformations.
  - `ResolveToEntityField(string)` - Resolves DTO field name to entity field name (returns `null` for invalid/unmapped fields)
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
  - `UseEfCoreLikeForCaseInsensitive` - Uses `EF.Functions.Like` for EF Core scenarios (default: `true`)
  - `PafisoSettings.Default` - Static property for global configuration

- **`IFieldNameResolver`** - Interface for resolving filter/sorting field names to property names. Implementations:
  - `DefaultFieldNameResolver` - Uses JSON naming policy and `[JsonPropertyName]` attributes
  - `PassThroughFieldNameResolver` - Returns field names unchanged

### Expression Builder Abstraction

Pluggable interfaces for building LINQ expressions, enabling different strategies for different runtimes (in-memory vs EF Core):

- **`IFilterExpressionBuilder`** - Interface for building filter expressions. Method: `BuildFilterExpression<T>(propName, paramName, op, value, caseSensitive, settings)`.
  - `DefaultFilterExpressionBuilder` - Default singleton implementation using `ExpressionUtilities`
  - `EfCoreExpressionBuilder` (in `Pafiso.EntityFrameworkCore`) - Uses `EF.Functions.Like` for case-insensitive string operations

- **`ISortingExpressionBuilder`** - Interface for building sorting expressions. Method: `BuildSortingExpression<T>(propName, paramName)`.
  - `DefaultSortingExpressionBuilder` - Default singleton implementation using `ExpressionUtilities`

### Fluent Builder Types (in `Pafiso.AspNetCore`)

- **`SearchParametersBuilder<TEntity>`** - Builder for creating `SearchParameters` from `IQueryCollection`. Methods:
  - `WithPaging()` - Enables paging
  - `WithFiltering<TMapping>()` - Adds filtering with a DTO mapping model
  - `WithFiltering<TMapping>(IFilterExpressionBuilder)` - Adds filtering with a custom expression builder
  - `WithFiltering<TMapping>(IFilterExpressionBuilder, bool defaultCaseSensitive)` - Adds filtering with custom expression builder and case sensitivity
  - `WithSorting<TMapping>()` - Adds sorting with a DTO mapping model
  - `WithSorting<TMapping>(ISortingExpressionBuilder)` - Adds sorting with a custom expression builder
  - `Build()` - Builds the `SearchParameters` instance

- **`FilterOptionsBuilder<TMapping, TEntity>`** - Builder for configuring filter field mappings. Methods:
  - `Map(mappingField, entityField)` - Maps a DTO field to entity field
  - `MapWithTransform<TValue>(mappingField, entityField, transformer)` - Maps with value transformation

- **`SortingOptionsBuilder<TMapping, TEntity>`** - Builder for configuring sorting field mappings. Methods:
  - `Map(mappingField, entityField)` - Maps a DTO field to entity field

### Queryable Wrappers

- **`PafisoQueryable<T>`** (in `Pafiso.AspNetCore`) - Wraps `IQueryable<T>` with Pafiso operations. Exposes `PagedQuery`, `CountQuery`, `Paging`, and `ToPagedList()` (sync).

- **`PafisoQueryableAsync<T>`** (in `Pafiso.EntityFrameworkCore`) - Wraps `PafisoQueryable<T>` for async EF Core operations. Provides `ToPagedListAsync()`.

### Result Types

- **`PagedList<T>`** - Materialized result containing `Entries` (IList<T>), `TotalEntries` (int), `PageNumber` (int), and `PageSize` (int). Implements `IList<T>`. Has custom JSON serializer.

### Key Dependencies

- **LinqKit** - Used for `PredicateBuilder` to compose OR predicates across multiple filter fields

## Additional Packages

### Pafiso.EntityFrameworkCore

Provides EF Core-specific expression building and async support:

- **`EfCoreExpressionBuilder`** - Singleton `IFilterExpressionBuilder` that provides `EF.Functions.Like` support for case-insensitive string operations. Accessed via `EfCoreExpressionBuilder.Instance`.

- **`EfFilteringExtensions`** - Extension methods on `SearchParametersBuilder<TEntity>`:
  - `WithEfFiltering<TMapping>()` - Shorthand for `WithFiltering<TMapping>(EfCoreExpressionBuilder.Instance)`
  - `WithEfFiltering<TMapping>(EfFilteringSettings)` - With configurable case sensitivity settings

- **`EfFilteringSettings`** - Configuration for EF Core filtering:
  - `CaseSensitive` (default: `false`) - Whether string comparisons default to case-sensitive

- **`PafisoQueryableAsync<T>`** - Async wrapper providing `ToPagedListAsync(CancellationToken)`.

- **`QueryableExtensions.WithPafiso<T>()`** - Returns `PafisoQueryableAsync<T>` (overrides `Pafiso.AspNetCore` version for async support).

### Pafiso.AspNetCore

Provides ASP.NET Core integration:

- **`QueryCollectionSearchParametersExtensions`** - `ToSearchParameters<TEntity>(configure, settings?)` extension for `IQueryCollection`. Fluent builder-based API.

- **`QueryCollectionExtensions`** - Legacy `ToSearchParameters<TMapping, TEntity>(mapper)` extension for `IQueryCollection`. Takes a mapper instance directly.

- **`QueryableExtensions`** - `WithPafiso<T>()` extension for `IQueryable<T>`. Accepts either `IQueryCollection` with optional configure action, or pre-built `SearchParameters`. Returns `PafisoQueryable<T>`.

- **`ServiceCollectionExtensions`** - DI registration methods (using C# extension member syntax):
  - `AddPafiso(configure?)` - Registers `PafisoSettings` as singleton, auto-detects MVC JSON settings
  - `AddPafiso(settings)` - Registers pre-configured `PafisoSettings`
  - `AddFieldMapper<TMapping, TEntity>(configure?)` - Registers a field mapper as singleton
  - `AddFieldMapperWithJsonOptions<TMapping, TEntity>(configure?)` - Registers a field mapper with JSON options integration

## Recommended Usage Pattern

### Style 1: Fluent Builder (Recommended)

```csharp
using Pafiso.EntityFrameworkCore;

[HttpGet]
public async Task<PagedList<Product>> GetProducts() {
    return await _dbContext.Products
        .WithPafiso(Request.Query, configure: opt => {
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

### Style 2: SearchParameters (Reusable)

```csharp
using Pafiso.AspNetCore;
using Pafiso.EntityFrameworkCore;

var searchParams = Request.Query.ToSearchParameters<Product>(builder => {
    builder.WithPaging();
    builder.WithFiltering<ProductFilterDto>()
        .Map(dto => dto.ProductId, entity => entity.Id);
});

var result = await _dbContext.Products
    .WithPafiso(searchParams)
    .ToPagedListAsync();
```

### Style 3: Manual with Mapper (Legacy)

```csharp
var mapper = new FieldMapper<ProductFilterDto, Product>(settings)
    .Map(dto => dto.ProductId, entity => entity.Id);

var searchParams = Request.Query.ToSearchParameters<ProductFilterDto, Product>(mapper);
var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(_dbContext.Products);

var totalCount = await countQuery.CountAsync();
var items = await pagedQuery.ToListAsync();
```

### EF Core Filtering with WithEfFiltering

```csharp
using Pafiso.EntityFrameworkCore;

return await _dbContext.Products
    .WithPafiso(Request.Query, configure: opt => {
        opt.WithPaging();
        opt.WithEfFiltering<ProductFilterDto>()
            .Map(dto => dto.ProductId, entity => entity.Id);
        opt.WithSorting<ProductFilterDto>();
    })
    .ToPagedListAsync();
```

Example query string:
```
GET /api/products?skip=0&take=10&filters[0][fields]=productName&filters[0][op]=contains&filters[0][val]=laptop&sortings[0][prop]=minPrice&sortings[0][ord]=asc
```

### Setup in Program.cs

```csharp
// Register Pafiso with auto-detection of JSON settings
builder.Services.AddPafiso();

// Or configure manually
builder.Services.AddPafiso(settings => {
    settings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// Register field mappers in DI
builder.Services.AddFieldMapper<ProductFilterDto, Product>(mapper => {
    mapper.Map(dto => dto.ProductId, entity => entity.Id);
    mapper.Map(dto => dto.ProductName, entity => entity.Name);
});
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
- `NoMapperTests.cs` - Tests for Filter/Sorting without mapper (serialization, equality)

### Package-Specific Tests
- `EfCoreExpressionBuilderTest.cs` - EF Core expression builder tests (in Pafiso.EntityFrameworkCore.Tests)
- `EfFilteringExtensionsTest.cs` - EF Core filtering extensions tests (in Pafiso.EntityFrameworkCore.Tests)
- `PagedQueryableAsyncTest.cs` - EF Core async paging tests (in Pafiso.EntityFrameworkCore.Tests)
- `QueryCollectionExtensionsTest.cs` - ASP.NET Core query collection tests (in Pafiso.AspNetCore.Tests)
- `SearchParametersExtensionsTest.cs` - SearchParameters builder extensions tests (in Pafiso.AspNetCore.Tests)
- `WithPafisoExtensionTest.cs` - WithPafiso fluent API tests (in Pafiso.AspNetCore.Tests)
- `ServiceCollectionExtensionsTest.cs` - ASP.NET Core DI tests (in Pafiso.AspNetCore.Tests)
