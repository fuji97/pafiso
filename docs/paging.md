# Paging

Paging (also known as pagination) allows you to split large result sets into smaller, manageable chunks. This is essential for performance and user experience when dealing with large datasets.

## Table of Contents

- [Creating Paging](#creating-paging)
  - [From Page Number and Size](#from-page-number-and-size)
  - [From Skip and Take](#from-skip-and-take)
- [Paging Properties](#paging-properties)
- [Applying Paging](#applying-paging)
- [Page Navigation](#page-navigation)
- [Result Types](#result-types)
- [Serialization](#serialization)
- [Examples](#examples)

## Creating Paging

Pafiso supports two ways to create paging: page-based (traditional pagination) and offset-based (skip/take).

### From Page Number and Size

This is the most common approach, using human-friendly page numbers:

```csharp
using Pafiso;

// Page 0 (first page), 20 items per page
var paging = Paging.FromPaging(page: 0, pageSize: 20);

// Page 2, 50 items per page
var paging = Paging.FromPaging(page: 2, pageSize: 50);
```

**Note**: Pafiso uses 0-based page numbering by default (first page is 0).

### From Skip and Take

You can also create paging using skip/take semantics:

```csharp
// Skip first 20 items, take next 10
var paging = Paging.FromSkipTake(skip: 20, take: 10);

// Get first 50 items
var paging = Paging.FromSkipTake(skip: 0, take: 50);
```

## Paging Properties

Once created, a `Paging` instance provides several properties:

```csharp
var paging = Paging.FromPaging(page: 2, pageSize: 20);

Console.WriteLine(paging.Skip);     // 40 (items to skip)
Console.WriteLine(paging.Take);     // 20 (items to take)
Console.WriteLine(paging.Page);     // 2 (zero-based page number)
Console.WriteLine(paging.PageSize); // 20 (items per page)
```

### Property Reference

| Property | Type | Description |
|----------|------|-------------|
| `Skip` | `int` | Number of items to skip |
| `Take` | `int` | Number of items to take |
| `Page` | `int` | Zero-based page number (computed from Skip/Take) |
| `PageSize` | `int` | Items per page (alias for Take) |

## Applying Paging

### Using Extension Methods

The most convenient way to apply paging:

```csharp
using Pafiso.Util;

var paging = Paging.FromPaging(page: 0, pageSize: 20);
var firstPage = products.Paging(paging).ToList();
```

### Using ApplyToIQueryable Method

```csharp
var paging = Paging.FromPaging(page: 1, pageSize: 25);
var secondPage = paging.ApplyToIQueryable(products).ToList();
```

### With LINQ

Since paging translates to Skip/Take, you can also apply it manually:

```csharp
var paging = Paging.FromPaging(page: 0, pageSize: 10);

// These are equivalent:
var results1 = products.Skip(paging.Skip).Take(paging.Take).ToList();
var results2 = paging.ApplyToIQueryable(products).ToList();
```

## Page Navigation

Pafiso provides operators for easy page navigation:

```csharp
var currentPage = Paging.FromPaging(page: 2, pageSize: 20);

// Next page
var nextPage = currentPage + 1;
Console.WriteLine(nextPage.Page); // 3

// Previous page
var prevPage = currentPage - 1;
Console.WriteLine(prevPage.Page); // 1

// Jump multiple pages forward
var futurePages = currentPage + 5;
Console.WriteLine(futurePages.Page); // 7

// Jump multiple pages backward
var pastPages = currentPage - 2;
Console.WriteLine(pastPages.Page); // 0
```

### Navigation Example

```csharp
public class PaginationHelper<T> {
    private readonly IQueryable<T> _query;
    private readonly int _pageSize;
    
    public PaginationHelper(IQueryable<T> query, int pageSize = 20) {
        _query = query;
        _pageSize = pageSize;
    }
    
    public List<T> GetPage(int pageNumber) {
        var paging = Paging.FromPaging(pageNumber, _pageSize);
        return paging.ApplyToIQueryable(_query).ToList();
    }
    
    public List<T> GetNextPage(Paging currentPaging) {
        var nextPaging = currentPaging + 1;
        return nextPaging.ApplyToIQueryable(_query).ToList();
    }
    
    public List<T> GetPreviousPage(Paging currentPaging) {
        if (currentPaging.Page <= 0) {
            throw new InvalidOperationException("Already on first page");
        }
        
        var prevPaging = currentPaging - 1;
        return prevPaging.ApplyToIQueryable(_query).ToList();
    }
}
```

## Result Types

When using paging, you often need to know the total number of items. Pafiso provides specialized result types:

### PagedQueryable<T>

Used with `IQueryable<T>`:

```csharp
using Pafiso.Enumerables;

var paging = Paging.FromPaging(page: 0, pageSize: 20);

// Manual creation
var pagedResult = new PagedQueryable<Product>(
    query: products.Skip(paging.Skip).Take(paging.Take),
    totalEntries: products.Count()
);

Console.WriteLine($"Total items: {pagedResult.TotalEntries}");
Console.WriteLine($"Items in this page: {pagedResult.Count()}");

foreach (var product in pagedResult) {
    Console.WriteLine(product.Name);
}
```

### PagedEnumerable<T>

Used with `IEnumerable<T>`:

```csharp
var paging = Paging.FromPaging(page: 0, pageSize: 20);
var list = products.ToList();

var pagedResult = new PagedEnumerable<Product>(
    enumerable: list.Skip(paging.Skip).Take(paging.Take),
    totalEntries: list.Count
);

Console.WriteLine($"Total items: {pagedResult.TotalEntries}");
```

### PagedList<T>

A concrete list implementation:

```csharp
var paging = Paging.FromPaging(page: 0, pageSize: 20);
var list = products.ToList();

var pagedList = new PagedList<Product>(
    items: list.Skip(paging.Skip).Take(paging.Take),
    totalEntries: list.Count
);

// Can be used like a regular list
pagedList.Add(new Product());
Console.WriteLine(pagedList[0].Name);
```

### Using with SearchParameters

The easiest way to get paged results with total count:

```csharp
var searchParams = new SearchParameters(Paging.FromPaging(0, 20));

// WithSearchParameters returns PagedQueryable<T>
var pagedQueryable = products.WithSearchParameters(searchParams);

// ToPagedListAsync() executes and materializes with TotalEntries
var result = await pagedQueryable.ToPagedListAsync();

Console.WriteLine($"Showing {result.Entries.Count} of {result.TotalEntries} items");
```

## Serialization

Paging can be serialized to and from dictionaries:

### To Dictionary

```csharp
var paging = Paging.FromPaging(page: 2, pageSize: 25);
IDictionary<string, string> dict = paging.ToDictionary();

// Results in:
// {
//   "skip": "50",
//   "take": "25"
// }
```

### From Dictionary

```csharp
var dict = new Dictionary<string, string> {
    ["skip"] = "0",
    ["take"] = "20"
};

var paging = Paging.FromDictionary(dict);
// Returns Paging with Skip=0, Take=20

// If keys are missing, returns null
var emptyDict = new Dictionary<string, string>();
var result = Paging.FromDictionary(emptyDict);
// result is null
```

## Examples

### Example 1: Basic Pagination

```csharp
public class ProductService {
    private readonly DbContext _dbContext;
    
    public ProductService(DbContext dbContext) {
        _dbContext = dbContext;
    }
    
    public async Task<PagedList<Product>> GetProducts(int page, int pageSize = 20) {
        var searchParams = new SearchParameters(Paging.FromPaging(page, pageSize));
        
        return await _dbContext.Products
            .WithSearchParameters(searchParams)
            .ToPagedListAsync();
    }
}

// Usage
var service = new ProductService(dbContext);
var page1 = await service.GetProducts(page: 0);

Console.WriteLine($"Page 1");
Console.WriteLine($"Total products: {page1.TotalEntries}");

foreach (var product in page1.Entries) {
    Console.WriteLine(product.Name);
}
```

### Example 2: API Response with Pagination Metadata

```csharp
public class PaginatedResponse<T> {
    public List<T> Items { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public bool HasPreviousPage => CurrentPage > 0;
    public bool HasNextPage => CurrentPage < TotalPages - 1;
}

[HttpGet]
public async Task<IActionResult> GetProducts(
    [FromQuery] int page = 0, 
    [FromQuery] int pageSize = 20,
    [FromServices] PafisoSettings settings) {
    
    var searchParams = new SearchParameters(Paging.FromPaging(page, pageSize));
    
    var result = await _dbContext.Products
        .WithSearchParameters(searchParams, settings)
        .ToPagedListAsync();
    
    var response = new PaginatedResponse<Product> {
        Items = result.Entries,
        CurrentPage = page,
        PageSize = pageSize,
        TotalItems = result.TotalEntries
    };
    
    return Ok(response);
}
```

### Example 3: Efficient Paging with Count Optimization

```csharp
// Avoid counting on every request by using an approximate count
public class EfficientPagination<T> {
    public async Task<PagedList<T>> GetPageAsync(
        IQueryable<T> query,
        int page,
        int pageSize,
        bool needsExactCount = false) {
        
        var searchParams = new SearchParameters(Paging.FromPaging(page, pageSize));
        
        if (needsExactCount) {
            // Exact count (slower) - standard approach
            return await query
                .WithSearchParameters(searchParams)
                .ToPagedListAsync();
        } else {
            // Approximate count (faster, good enough for large datasets)
            // Take one more item than page size to know if there are more pages
            var extendedPaging = Paging.FromPaging(page, pageSize + 1);
            var extendedParams = new SearchParameters(extendedPaging);
            
            var extendedResult = await query
                .WithSearchParameters(extendedParams)
                .ToPagedListAsync();
            
            var items = extendedResult.Entries.Take(pageSize).ToList();
            var hasMore = extendedResult.Entries.Count > pageSize;
            var approxCount = hasMore 
                ? page * pageSize + pageSize + 1  // At least one more page
                : page * pageSize + items.Count;  // Last page
            
            return new PagedList<T>(items, approxCount);
        }
    }
}
```

### Example 4: Cursor-Based Pagination Alternative

```csharp
// For very large datasets, cursor-based pagination can be more efficient
public class CursorPagination<T> where T : class {
    public List<T> GetPage(
        IQueryable<T> query,
        Expression<Func<T, object>> keySelector,
        object? lastSeenKey,
        int pageSize) {
        
        if (lastSeenKey != null) {
            // This is a simplified version - production code needs more complexity
            query = query.Where(x => EF.Property<object>(x, "Id") > lastSeenKey);
        }
        
        query = query.OrderBy(keySelector).Take(pageSize);
        
        return query.ToList();
    }
}

// However, for standard pagination, use Pafiso's built-in Paging
```

### Example 5: Dynamic Page Size with Limits

```csharp
public class SafePagination {
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;
    
    public static Paging CreateSafePaging(int? page, int? pageSize) {
        // Ensure page is non-negative
        var safePage = Math.Max(page ?? 0, 0);
        
        // Enforce page size limits
        var safePageSize = pageSize ?? DefaultPageSize;
        safePageSize = Math.Max(safePageSize, 1);        // At least 1
        safePageSize = Math.Min(safePageSize, MaxPageSize); // At most MaxPageSize
        
        return Paging.FromPaging(safePage, safePageSize);
    }
}

[HttpGet]
public async Task<IActionResult> GetProducts(
    [FromQuery] int? page, 
    [FromQuery] int? pageSize,
    [FromServices] PafisoSettings settings) {
    
    var paging = SafePagination.CreateSafePaging(page, pageSize);
    var searchParams = new SearchParameters(paging);
    
    var result = await _dbContext.Products
        .WithSearchParameters(searchParams, settings)
        .ToPagedListAsync();
    
    return Ok(new {
        Items = result.Entries,
        Page = paging.Page,
        PageSize = paging.PageSize,
        TotalItems = result.TotalEntries
    });
}
```

### Example 6: Infinite Scroll Implementation

```csharp
public class InfiniteScrollService {
    private readonly DbContext _dbContext;
    private const int ItemsPerLoad = 20;
    
    public InfiniteScrollService(DbContext dbContext) {
        _dbContext = dbContext;
    }
    
    public async Task<InfiniteScrollResult<Product>> LoadMoreAsync(int offset) {
        var paging = Paging.FromSkipTake(skip: offset, take: ItemsPerLoad);
        var query = _dbContext.Products.OrderBy(p => p.CreatedDate);
        
        var items = await query
            .Skip(paging.Skip)
            .Take(paging.Take + 1) // Get one extra to check if more exist
            .ToListAsync();
        
        var hasMore = items.Count > ItemsPerLoad;
        if (hasMore) {
            items = items.Take(ItemsPerLoad).ToList();
        }
        
        return new InfiniteScrollResult<Product> {
            Items = items,
            NextOffset = offset + items.Count,
            HasMore = hasMore
        };
    }
}

public class InfiniteScrollResult<T> {
    public List<T> Items { get; set; }
    public int NextOffset { get; set; }
    public bool HasMore { get; set; }
}
```

### Example 7: Combining Paging with Filtering and Sorting

```csharp
[HttpGet]
public async Task<IActionResult> SearchProducts(
    [FromQuery] string? search,
    [FromQuery] string? sortBy,
    [FromQuery] int page = 0,
    [FromQuery] int pageSize = 20,
    [FromServices] PafisoSettings settings) {
    
    var searchParams = new SearchParameters(Paging.FromPaging(page, pageSize));
    
    // Apply filtering
    if (!string.IsNullOrEmpty(search)) {
        searchParams.AddFilters(new Filter("Name", FilterOperator.Contains, search));
    }
    
    // Apply sorting
    if (!string.IsNullOrEmpty(sortBy)) {
        var order = sortBy.ToLower() switch {
            "price" => SortOrder.Ascending,
            "name" => SortOrder.Ascending,
            _ => SortOrder.Descending
        };
        searchParams.AddSorting(new Sorting(sortBy, order));
    }
    
    // Apply all together
    var result = await _dbContext.Products
        .WithSearchParameters(searchParams, settings)
        .ToPagedListAsync();
    
    return Ok(new {
        Items = result.Entries,
        Page = page,
        PageSize = pageSize,
        TotalItems = result.TotalEntries,
        TotalPages = (int)Math.Ceiling((double)result.TotalEntries / pageSize)
    });
}
```

## Best Practices

1. **Set reasonable page size limits** - Prevent users from requesting thousands of items at once
2. **Use default values** - Provide sensible defaults (e.g., page 0, 20 items per page)
3. **Return metadata** - Include total count, page number, and page size in responses
4. **Consider performance** - Counting can be expensive; use approximate counts for large datasets
5. **Order before paging** - Always apply sorting before paging for consistent results
6. **Validate inputs** - Ensure page numbers and sizes are non-negative and within limits

## Performance Considerations

### Efficient Counting

```csharp
// For large datasets, counting can be slow
// Consider these alternatives:

// 1. Cache the total count
var cachedCount = _cache.GetOrCreate("products_count", entry => {
    entry.SlidingExpiration = TimeSpan.FromMinutes(5);
    return _dbContext.Products.Count();
});

// 2. Use approximate counts for display purposes
var approxCount = _dbContext.Products.Take(10001).Count();
var displayCount = approxCount > 10000 ? "10,000+" : approxCount.ToString();

// 3. Only count when necessary
var needsCount = page == 0; // Only count on first page
```

## Next Steps

- Combine paging with [Filtering](filtering.md) and [Sorting](sorting.md)
- Use [SearchParameters](search-parameters.md) to manage all three together
- Learn about [ASP.NET Core Integration](aspnetcore-integration.md) for automatic query string parsing
