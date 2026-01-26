# CLAUDE.md

This file provides guidance to AI agents when working with code in this repository.

## Project Overview

Pafiso is a .NET 10 library for serializing, deserializing, and applying Paging, Filtering, and Sorting to `IQueryable<T>` and `IEnumerable<T>` collections. It enables building dynamic queries from expression trees or dictionary representations (useful for query string parameters).

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

- **`SearchParameters`** - Combines Paging, Sorting, and Filter into a single object. **Primary method**: Apply to queries via `WithSearchParameters()` extension method, which returns a transient `PagedQueryable<T>` wrapper (no query execution). Call `ToPagedListAsync()` or `ToPagedList()` to execute queries and get results with `TotalEntries` and `Entries`. Alternative: `ApplyToIQueryable<T>()` for advanced scenarios requiring separate count/paged queries. Serializes to/from dictionary for query string support.

- **`Filter` / `Filter<T>`** - Represents a filter condition with field(s), operator, value, and case sensitivity. Multiple fields create OR conditions. Create from expressions via `Filter.FromExpression<T>(x => x.Age > 20)`.

- **`Sorting` / `Sorting<T>`** - Represents sort order for a property. Create from expressions via `Sorting.FromExpression<T>(x => x.Name, SortOrder.Ascending)`.

- **`Paging`** - Represents pagination as skip/take. Create via `Paging.FromPaging(page, pageSize)` or `Paging.FromSkipTake(skip, take)`.

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

- **`PagedQueryable<T>`** / **`PagedEnumerable<T>`** - Transient query wrappers returned by `WithSearchParameters()`. No query execution occurs until materialization methods are called.
- **`PagedList<T>`** - Materialized result containing `Entries` (List<T>) and `TotalEntries` (int). Obtained by calling `ToPagedListAsync()` or `ToPagedList()` on a `PagedQueryable<T>`.

### Extension Methods (in `Pafiso.Util`)

- `IQueryable<T>.Where(Filter)` - Apply filter to queryable
- `IQueryable<T>.OrderBy(Sorting)` / `ThenBy(Sorting)` - Apply sorting
- `IQueryable<T>.Paging(Paging)` - Apply pagination

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

- **`QueryCollectionExtensions`** - `ToSearchParameters()` extension for `IQueryCollection`. Takes optional `PafisoSettings` parameter.
- **`ServiceCollectionExtensions`** - `AddPafiso()` for DI registration, auto-detects MVC JSON settings and registers `PafisoSettings` as singleton

## Recommended Usage Pattern

### ASP.NET Core Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase {
    private readonly ApplicationDbContext _dbContext;
    private readonly PafisoSettings _settings;
    
    // Inject PafisoSettings via constructor (registered by AddPafiso())
    public ProductsController(ApplicationDbContext dbContext, PafisoSettings settings) {
        _dbContext = dbContext;
        _settings = settings;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetProducts() {
        // Parse query string with settings
        var searchParams = Request.Query.ToSearchParameters(_settings);
        
        // WithSearchParameters returns PagedQueryable (no execution)
        var pagedQueryable = _dbContext.Products.WithSearchParameters(searchParams);
        
        // ToPagedListAsync executes queries and materializes result
        var result = await pagedQueryable.ToPagedListAsync();
        
        return Ok(new {
            TotalCount = result.TotalEntries,
            Items = result.Entries
        });
    }
}
```

### Getting PafisoSettings from DI

**Option 1: Constructor Injection (Recommended)**
```csharp
public class MyController : ControllerBase {
    private readonly PafisoSettings _settings;
    
    public MyController(PafisoSettings settings) {
        _settings = settings;
    }
}
```

**Option 2: Method Injection**
```csharp
[HttpGet]
public async Task<IActionResult> GetProducts([FromServices] PafisoSettings settings) {
    var searchParams = Request.Query.ToSearchParameters(settings);
    // ...
}
```

**Option 3: No DI (uses PafisoSettings.Default)**
```csharp
[HttpGet]
public async Task<IActionResult> GetProducts() {
    var searchParams = Request.Query.ToSearchParameters(); // Uses PafisoSettings.Default
    // ...
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

Tests use NUnit 4 with Shouldly for assertions. Test files mirror the core types: `FilterTest.cs`, `SortingTest.cs`, `PagingTest.cs`, `SearchParameterTest.cs`.

Additional test files for settings:
- `PafisoSettingsTest.cs` - Tests for settings class
- `FieldNameResolverTest.cs` - Tests for field name resolution
- `FilterWithSettingsTest.cs` - Filter integration tests with settings
- `SortingWithSettingsTest.cs` - Sorting integration tests with settings
- `SearchParametersWithSettingsTest.cs` - SearchParameters integration tests
- `EfCoreExpressionBuilderTest.cs` - EF Core expression builder tests (in Pafiso.EntityFrameworkCore.Tests)
- `ServiceCollectionExtensionsTest.cs` - ASP.NET Core DI tests (in Pafiso.AspNetCore.Tests)
