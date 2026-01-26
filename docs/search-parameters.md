# SearchParameters

`SearchParameters` is the central class in Pafiso that combines paging, filtering, and sorting into a single, cohesive object. It provides a convenient way to manage complex queries and is designed for easy serialization to/from query strings.

## Table of Contents

- [Overview](#overview)
- [Creating SearchParameters](#creating-searchparameters)
- [Building with Fluent API](#building-with-fluent-api)
- [Applying to Queries](#applying-to-queries)
- [Working with Results](#working-with-results)
- [Combining SearchParameters](#combining-searchparameters)
- [Serialization](#serialization)
- [Examples](#examples)

## Overview

SearchParameters brings together three core concepts:

- **Paging** - Controls which page of results to return
- **Filters** - Defines conditions to filter data (AND logic between filters)
- **Sorting** - Specifies ordering of results (multi-level sorting supported)

```csharp
var searchParams = new SearchParameters(paging)
    .AddFilters(filter1, filter2)  // AND between filters
    .AddSorting(sort1, sort2);     // Multi-level sorting
```

## Creating SearchParameters

### Empty SearchParameters

```csharp
// Create an empty instance
var searchParams = new SearchParameters();

// This will return all results (no filtering, sorting, or paging)
var pagedQueryable = products.WithSearchParameters(searchParams);
var result = await pagedQueryable.ToPagedListAsync();
```

### With Paging Only

```csharp
// Create with paging
var paging = Paging.FromPaging(page: 0, pageSize: 20);
var searchParams = new SearchParameters(paging);
```

### Setting Properties Directly

```csharp
var searchParams = new SearchParameters {
    Paging = Paging.FromPaging(0, 20),
    Filters = new List<Filter> {
        Filter.FromExpression<Product>(x => x.Price > 100)
    },
    Sortings = new List<Sorting> {
        Sorting.FromExpression<Product>(x => x.Name, SortOrder.Ascending)
    }
};
```

## Building with Fluent API

The recommended approach is using the fluent API for readability:

```csharp
var searchParams = new SearchParameters(Paging.FromPaging(0, 20))
    .AddFilters(
        Filter.FromExpression<Product>(x => x.Price > 100),
        Filter.FromExpression<Product>(x => x.Category == "Electronics")
    )
    .AddSorting(
        Sorting.FromExpression<Product>(x => x.Name, SortOrder.Ascending),
        Sorting.FromExpression<Product>(x => x.Price, SortOrder.Descending)
    );
```

### Building Incrementally

```csharp
var searchParams = new SearchParameters();

// Add paging
searchParams.Paging = Paging.FromPaging(0, 20);

// Add filters one at a time
searchParams.AddFilters(Filter.FromExpression<Product>(x => x.Price > 50));
searchParams.AddFilters(Filter.FromExpression<Product>(x => x.InStock == true));

// Add sorting
searchParams.AddSorting(Sorting.FromExpression<Product>(x => x.Name, SortOrder.Ascending));
```

## Applying to Queries

### Using WithSearchParameters (Recommended)

The recommended approach is using the `WithSearchParameters()` extension method that returns a `PagedQueryable<T>` wrapper. This is a lightweight, transient class that doesn't execute queries until you call `ToPagedListAsync()` or `ToPagedList()`:

```csharp
var searchParams = new SearchParameters(Paging.FromPaging(0, 20))
    .AddFilters(Filter.FromExpression<Product>(x => x.Price > 100));

// Create the PagedQueryable - no query execution yet
var pagedQueryable = products.WithSearchParameters(searchParams);

// Execute queries and get results
var result = await pagedQueryable.ToPagedListAsync();

Console.WriteLine($"Showing {result.Entries.Count} of {result.TotalEntries} items");
foreach (var product in result.Entries) {
    Console.WriteLine($"{product.Name}: ${product.Price}");
}
```

### Using ApplyToIQueryable (Advanced)

For advanced scenarios where you need separate access to the count and paged queries, use `ApplyToIQueryable`:

```csharp
var searchParams = new SearchParameters(Paging.FromPaging(0, 20))
    .AddFilters(Filter.FromExpression<Product>(x => x.Price > 100));

var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(products);

// Get total count (before paging)
int totalCount = await countQuery.CountAsync();

// Get paged results
var results = await pagedQuery.ToListAsync();

Console.WriteLine($"Showing {results.Count} of {totalCount} items");
```

### With Settings

```csharp
var settings = new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    UseJsonPropertyNameAttributes = true
};

var searchParams = SearchParameters.FromDictionary(queryStringDict);
var pagedQueryable = products.WithSearchParameters(searchParams, settings);
var result = await pagedQueryable.ToPagedListAsync();
```

### With Field Restrictions (Action)

```csharp
var searchParams = SearchParameters.FromDictionary(Request.Query.ToDictionary());

var pagedQueryable = products.WithSearchParameters(
    searchParams,
    restrictions => restrictions
        .AllowFiltering<Product>(x => x.Name, x => x.Price, x => x.Category)
        .AllowSorting<Product>(x => x.Name, x => x.Price)
        .BlockFiltering<Product>(x => x.InternalCost)
);

var result = await pagedQueryable.ToPagedListAsync();
```

### With Field Restrictions (Pre-configured)

```csharp
var restrictions = new FieldRestrictions()
    .AllowFiltering<Product>(x => x.Name, x => x.Price)
    .AllowSorting<Product>(x => x.Name, x => x.Price);

var pagedQueryable = products.WithSearchParameters(searchParams, restrictions);
var result = await pagedQueryable.ToPagedListAsync();
```

### With Both Restrictions and Settings

```csharp
var settings = new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

var pagedQueryable = products.WithSearchParameters(
    searchParams,
    restrictions => restrictions
        .AllowFiltering<Product>(x => x.Name, x => x.Price),
    settings
);

var result = await pagedQueryable.ToPagedListAsync();
```

## Working with Results

### Using WithSearchParameters (Recommended)

The recommended approach creates a `PagedQueryable<T>` that defers execution until you materialize it:

```csharp
var searchParams = new SearchParameters(Paging.FromPaging(0, 20));

// Create PagedQueryable - no queries executed yet
var pagedQueryable = products.WithSearchParameters(searchParams);

// Execute queries and get result with TotalEntries
var result = await pagedQueryable.ToPagedListAsync();

Console.WriteLine($"Total: {result.TotalEntries}");
foreach (var item in result.Entries) {
    // Process items
}
```

### Using ApplyToIQueryable Directly

For advanced scenarios where you need separate control over count and paged queries:

```csharp
var searchParams = new SearchParameters(Paging.FromPaging(0, 20));

// Get both queries
var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(products);

// Execute queries separately
int total = await countQuery.CountAsync();
var items = await pagedQuery.ToListAsync();
```

## Combining SearchParameters

You can combine multiple SearchParameters instances using the `+` operator:

```csharp
// Base parameters
var baseParams = new SearchParameters(Paging.FromPaging(0, 20))
    .AddSorting(Sorting.FromExpression<Product>(x => x.Name, SortOrder.Ascending));

// User-specific filters
var userParams = new SearchParameters()
    .AddFilters(Filter.FromExpression<Product>(x => x.Category == "Electronics"));

// Combine them
var combined = baseParams + userParams;

// Result has:
// - Paging from baseParams (left side takes precedence)
// - Sorting from baseParams
// - Filters from userParams
// - All sortings and filters are merged
```

### Operator Behavior

When combining SearchParameters with `+`:

- **Paging**: Left side takes precedence (if null, uses right side)
- **Filters**: Concatenated (all filters from both sides)
- **Sortings**: Concatenated (all sortings from both sides)

```csharp
var left = new SearchParameters(Paging.FromPaging(0, 20))
    .AddFilters(Filter.FromExpression<Product>(x => x.Price > 100))
    .AddSorting(Sorting.FromExpression<Product>(x => x.Name, SortOrder.Ascending));

var right = new SearchParameters(Paging.FromPaging(0, 50))
    .AddFilters(Filter.FromExpression<Product>(x => x.InStock == true))
    .AddSorting(Sorting.FromExpression<Product>(x => x.Price, SortOrder.Descending));

var combined = left + right;

// combined has:
// Paging: page 0, size 20 (from left)
// Filters: [Price > 100, InStock == true] (both)
// Sortings: [Name ASC, Price DESC] (both)
```

## Serialization

SearchParameters can be serialized to and from dictionaries, making them perfect for query strings:

### To Dictionary

```csharp
var searchParams = new SearchParameters(Paging.FromPaging(0, 20))
    .AddFilters(
        Filter.FromExpression<Product>(x => x.Name.Contains("laptop")),
        Filter.FromExpression<Product>(x => x.Price > 100)
    )
    .AddSorting(
        Sorting.FromExpression<Product>(x => x.Price, SortOrder.Descending)
    );

IDictionary<string, string> dict = searchParams.ToDictionary();

// Results in something like:
// {
//   "skip": "0",
//   "take": "20",
//   "filters[0][fields]": "Name",
//   "filters[0][op]": "contains",
//   "filters[0][val]": "laptop",
//   "filters[1][fields]": "Price",
//   "filters[1][op]": "gt",
//   "filters[1][val]": "100",
//   "sortings[0][prop]": "Price",
//   "sortings[0][ord]": "desc"
// }
```

### From Dictionary

```csharp
var dict = new Dictionary<string, string> {
    ["skip"] = "0",
    ["take"] = "20",
    ["filters[0][fields]"] = "Name",
    ["filters[0][op]"] = "contains",
    ["filters[0][val]"] = "laptop",
    ["sortings[0][prop]"] = "Price",
    ["sortings[0][ord]"] = "desc"
};

var searchParams = SearchParameters.FromDictionary(dict);
```

### From Dictionary with Settings

```csharp
var settings = new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

var searchParams = SearchParameters.FromDictionary(dict, settings);
// Settings will be used when ApplyToIQueryable is called
```

## Examples

### Example 1: Complete Product Search

```csharp
public class ProductSearchService {
    private readonly DbContext _dbContext;
    
    public PagedResult<Product> SearchProducts(ProductSearchRequest request) {
        // Build search parameters
        var searchParams = new SearchParameters(
            Paging.FromPaging(request.Page, request.PageSize)
        );
        
        // Add filters based on request
        if (!string.IsNullOrEmpty(request.SearchTerm)) {
            searchParams.AddFilters(
                Filter.FromExpression<Product>(x => x.Name.Contains(request.SearchTerm))
                    .AddField<Product>(x => x.Description)
            );
        }
        
        if (request.MinPrice.HasValue) {
            searchParams.AddFilters(
                Filter.FromExpression<Product>(x => x.Price >= request.MinPrice.Value)
            );
        }
        
        if (request.MaxPrice.HasValue) {
            searchParams.AddFilters(
                Filter.FromExpression<Product>(x => x.Price <= request.MaxPrice.Value)
            );
        }
        
        if (!string.IsNullOrEmpty(request.Category)) {
            searchParams.AddFilters(
                Filter.FromExpression<Product>(x => x.Category == request.Category)
            );
        }
        
        // Add sorting
        var sorting = request.SortBy?.ToLower() switch {
            "price" => Sorting.FromExpression<Product>(x => x.Price, 
                request.SortDescending ? SortOrder.Descending : SortOrder.Ascending),
            "name" => Sorting.FromExpression<Product>(x => x.Name, 
                request.SortDescending ? SortOrder.Descending : SortOrder.Ascending),
            _ => Sorting.FromExpression<Product>(x => x.CreatedDate, SortOrder.Descending)
        };
        searchParams.AddSorting(sorting);
        
        // Apply to query
        var pagedQueryable = _dbContext.Products
            .WithSearchParameters(searchParams);
        
        var result = await pagedQueryable.ToPagedListAsync();
        
        return new PagedResult<Product> {
            Items = result.Entries,
            TotalCount = result.TotalEntries,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

public class ProductSearchRequest {
    public int Page { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Category { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}
```

### Example 2: API Controller with Query String

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase {
    private readonly DbContext _dbContext;
    
    [HttpGet]
    public async Task<IActionResult> GetProducts() {
        // Parse query string into SearchParameters
        var searchParams = SearchParameters.FromDictionary(
            Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString())
        );
        
        // Apply with security restrictions
        var pagedQueryable = _dbContext.Products.WithSearchParameters(
            searchParams,
            restrictions => restrictions
                .AllowFiltering<Product>(
                    x => x.Name,
                    x => x.Category,
                    x => x.Price
                )
                .AllowSorting<Product>(
                    x => x.Name,
                    x => x.Price,
                    x => x.CreatedDate
                )
        );
        
        var result = await pagedQueryable.ToPagedListAsync();
        
        return Ok(new {
            TotalCount = result.TotalEntries,
            Items = result.Entries
        });
    }
}

// Example URL:
// GET /api/products?skip=0&take=20&filters[0][fields]=Price&filters[0][op]=gt&filters[0][val]=100&sortings[0][prop]=Name&sortings[0][ord]=asc
```

### Example 3: Reusable Search Templates

```csharp
public class SearchTemplates {
    // Template for active products
    public static SearchParameters ActiveProducts(int page = 0, int pageSize = 20) {
        return new SearchParameters(Paging.FromPaging(page, pageSize))
            .AddFilters(Filter.FromExpression<Product>(x => x.IsActive == true))
            .AddSorting(Sorting.FromExpression<Product>(x => x.Name, SortOrder.Ascending));
    }
    
    // Template for featured products
    public static SearchParameters FeaturedProducts(int page = 0, int pageSize = 20) {
        return new SearchParameters(Paging.FromPaging(page, pageSize))
            .AddFilters(
                Filter.FromExpression<Product>(x => x.IsFeatured == true),
                Filter.FromExpression<Product>(x => x.IsActive == true)
            )
            .AddSorting(Sorting.FromExpression<Product>(x => x.FeaturedOrder, SortOrder.Ascending));
    }
    
    // Template for new arrivals
    public static SearchParameters NewArrivals(int page = 0, int pageSize = 20) {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        return new SearchParameters(Paging.FromPaging(page, pageSize))
            .AddFilters(
                Filter.FromExpression<Product>(x => x.CreatedDate >= thirtyDaysAgo),
                Filter.FromExpression<Product>(x => x.IsActive == true)
            )
            .AddSorting(Sorting.FromExpression<Product>(x => x.CreatedDate, SortOrder.Descending));
    }
}

// Usage:
var featured = SearchTemplates.FeaturedProducts(page: 0, pageSize: 10);
var pagedQueryable = products.WithSearchParameters(featured);
var result = await pagedQueryable.ToPagedListAsync();
```

### Example 4: Combining Base Parameters with User Input

```csharp
[HttpGet("search")]
public async Task<IActionResult> SearchProducts([FromQuery] string? userSearch) {
    // Base parameters that always apply
    var baseParams = new SearchParameters()
        .AddFilters(
            Filter.FromExpression<Product>(x => x.IsActive == true),
            Filter.FromExpression<Product>(x => x.IsDeleted == false)
        )
        .AddSorting(Sorting.FromExpression<Product>(x => x.Name, SortOrder.Ascending));
    
    // User-provided search parameters
    var userParams = SearchParameters.FromDictionary(
        Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString())
    );
    
    // Combine: base filters + user filters
    var combined = baseParams + userParams;
    
    var pagedQueryable = _dbContext.Products.WithSearchParameters(combined);
    var result = await pagedQueryable.ToPagedListAsync();
    
    return Ok(new {
        TotalCount = result.TotalEntries,
        Items = result.Entries
    });
}
```

### Example 5: Cached Search Parameters

```csharp
public class SavedSearchService {
    private readonly IMemoryCache _cache;
    
    public void SaveSearch(string userId, string searchName, SearchParameters parameters) {
        var dict = parameters.ToDictionary();
        var json = JsonSerializer.Serialize(dict);
        _cache.Set($"search:{userId}:{searchName}", json);
    }
    
    public SearchParameters? LoadSearch(string userId, string searchName) {
        var json = _cache.Get<string>($"search:{userId}:{searchName}");
        if (json == null) return null;
        
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        return SearchParameters.FromDictionary(dict);
    }
}

// Usage:
var searchParams = new SearchParameters(Paging.FromPaging(0, 20))
    .AddFilters(Filter.FromExpression<Product>(x => x.Category == "Electronics"))
    .AddSorting(Sorting.FromExpression<Product>(x => x.Price, SortOrder.Descending));

// Save for later
searchService.SaveSearch("user123", "my-electronics-search", searchParams);

// Load and use
var saved = searchService.LoadSearch("user123", "my-electronics-search");
if (saved != null) {
    var pagedQueryable = products.WithSearchParameters(saved);
    var result = await pagedQueryable.ToPagedListAsync();
}
```

### Example 6: Default Search with Override

```csharp
public class ProductRepository {
    private readonly DbContext _dbContext;
    private readonly SearchParameters _defaultSearch;
    
    public ProductRepository(DbContext dbContext) {
        _dbContext = dbContext;
        
        // Set up default search
        _defaultSearch = new SearchParameters(Paging.FromPaging(0, 50))
            .AddFilters(Filter.FromExpression<Product>(x => x.IsActive == true))
            .AddSorting(Sorting.FromExpression<Product>(x => x.Name, SortOrder.Ascending));
    }
    
    public async Task<PagedList<Product>> GetProducts(SearchParameters? customSearch = null) {
        // Use custom search if provided, otherwise use default
        var searchParams = customSearch ?? _defaultSearch;
        
        var pagedQueryable = _dbContext.Products.WithSearchParameters(searchParams);
        return await pagedQueryable.ToPagedListAsync();
    }
}
```

## Best Practices

1. **Always provide paging** - Even if you expect few results, always paginate to prevent performance issues
2. **Use field restrictions** - Always apply restrictions in public APIs to prevent unauthorized field access
3. **Combine wisely** - When combining SearchParameters, be aware that left side paging takes precedence
4. **Validate inputs** - Validate page size, filter values, and field names before creating SearchParameters
5. **Consider caching** - Cache SearchParameters for common queries or saved searches
6. **Document operators** - Make sure API consumers know which filter operators are supported

## Execution Order

When SearchParameters is applied, operations execute in this order:

1. **Filters** - All filters are applied first (AND logic between filters)
2. **Sorting** - Results are sorted (multi-level sorting supported)
3. **Count query saved** - A reference to the query before paging is saved for counting
4. **Paging** - Skip and Take are applied last

```csharp
// Conceptual execution:
query = query.Where(filter1).Where(filter2)     // Filters
             .OrderBy(sort1).ThenBy(sort2)      // Sorting
var countQuery = query;                         // Save for counting
query = query.Skip(skip).Take(take);            // Paging
```

## Next Steps

- Learn about [Serialization](serialization.md) for query string integration
- Use [ASP.NET Core Integration](aspnetcore-integration.md) for automatic parsing
- Secure your API with [Field Restrictions](field-restrictions.md)
- Configure field name mapping in [Configuration & Settings](configuration.md)
