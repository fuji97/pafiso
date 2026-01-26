# Serialization

Pafiso provides robust serialization support for converting filters, sorting, paging, and search parameters to and from dictionaries. This is essential for working with HTTP query strings and storing search criteria.

## Table of Contents

- [Overview](#overview)
- [Filter Serialization](#filter-serialization)
- [Sorting Serialization](#sorting-serialization)
- [Paging Serialization](#paging-serialization)
- [SearchParameters Serialization](#searchparameters-serialization)
- [Query String Format](#query-string-format)
- [Working with URLs](#working-with-urls)
- [Examples](#examples)

## Overview

All core Pafiso types implement serialization through `ToDictionary()` and `FromDictionary()` methods:

- `Filter` / `Filter<T>`
- `Sorting` / `Sorting<T>`
- `Paging`
- `SearchParameters`

The serialization format is designed to be URL-friendly and follows common query string conventions.

## Filter Serialization

### Single Field Filter

```csharp
var filter = new Filter("Name", FilterOperator.Contains, "laptop");
var dict = filter.ToDictionary();

// Results in:
// {
//   "fields": "Name",
//   "op": "contains",
//   "val": "laptop"
// }

// Deserialize
var restored = Filter.FromDictionary(dict);
```

### Multi-Field Filter (OR Condition)

```csharp
var filter = new Filter(
    new[] { "Name", "Description", "Category" },
    FilterOperator.Contains,
    "computer"
);
var dict = filter.ToDictionary();

// Results in:
// {
//   "fields": "Name,Description,Category",  // Comma-separated
//   "op": "contains",
//   "val": "computer"
// }
```

### Filter with Case Sensitivity

```csharp
var filter = new Filter("Name", FilterOperator.Equals, "iPhone", caseSensitive: true);
var dict = filter.ToDictionary();

// Results in:
// {
//   "fields": "Name",
//   "op": "eq",
//   "val": "iPhone",
//   "case": "true"
// }
```

### Null/NotNull Filters

```csharp
var filter = new Filter("Description", FilterOperator.Null, null);
var dict = filter.ToDictionary();

// Results in:
// {
//   "fields": "Description",
//   "op": "null"
//   // Note: no "val" key when value is null
// }
```

### Filter Operator Serialization

Operators are serialized using short string codes:

| Operator | Serialized Value |
|----------|------------------|
| `Equals` | `eq` |
| `NotEquals` | `neq` |
| `GreaterThan` | `gt` |
| `LessThan` | `lt` |
| `GreaterThanOrEquals` | `gte` |
| `LessThanOrEquals` | `lte` |
| `Contains` | `contains` |
| `NotContains` | `ncontains` |
| `Null` | `null` |
| `NotNull` | `notnull` |

## Sorting Serialization

```csharp
var sorting = new Sorting("Price", SortOrder.Descending);
var dict = sorting.ToDictionary();

// Results in:
// {
//   "prop": "Price",
//   "ord": "desc"
// }

// Deserialize
var restored = Sorting.FromDictionary(dict);
```

### Sort Order Serialization

| Order | Serialized Value |
|-------|------------------|
| `Ascending` | `asc` |
| `Descending` | `desc` |

## Paging Serialization

```csharp
var paging = Paging.FromPaging(page: 2, pageSize: 25);
var dict = paging.ToDictionary();

// Results in:
// {
//   "skip": "50",   // Calculated as page * pageSize
//   "take": "25"
// }

// Deserialize
var restored = Paging.FromDictionary(dict);
// Returns null if "skip" or "take" keys are missing
```

## SearchParameters Serialization

SearchParameters combines all three types into a single dictionary:

### To Dictionary

```csharp
var searchParams = new SearchParameters(Paging.FromPaging(0, 20))
    .AddFilters(
        new Filter("Name", FilterOperator.Contains, "laptop"),
        new Filter("Price", FilterOperator.GreaterThan, "500")
    )
    .AddSorting(
        new Sorting("Price", SortOrder.Descending),
        new Sorting("Name", SortOrder.Ascending)
    );

var dict = searchParams.ToDictionary();

// Results in:
// {
//   "skip": "0",
//   "take": "20",
//   "filters[0][fields]": "Name",
//   "filters[0][op]": "contains",
//   "filters[0][val]": "laptop",
//   "filters[1][fields]": "Price",
//   "filters[1][op]": "gt",
//   "filters[1][val]": "500",
//   "sortings[0][prop]": "Price",
//   "sortings[0][ord]": "desc",
//   "sortings[1][prop]": "Name",
//   "sortings[1][ord]": "asc"
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

// searchParams now has:
// - Paging: skip=0, take=20
// - 1 Filter: Name contains "laptop"
// - 1 Sorting: Price descending
```

### Handling Missing Paging

```csharp
var dict = new Dictionary<string, string> {
    ["filters[0][fields]"] = "Name",
    ["filters[0][op]"] = "contains",
    ["filters[0][val]"] = "laptop"
};

var searchParams = SearchParameters.FromDictionary(dict);

// searchParams.Paging will be null
// No paging will be applied when using ApplyToIQueryable
```

## Query String Format

### Array Notation

Pafiso uses indexed array notation for collections:

```
filters[0][fields]=Name&filters[0][op]=contains&filters[0][val]=laptop
filters[1][fields]=Price&filters[1][op]=gt&filters[1][val]=100
```

This is a standard format supported by most web frameworks and is automatically parsed by ASP.NET Core's `IQueryCollection`.

### Complete Query String Example

```
?skip=0
&take=20
&filters[0][fields]=Name,Description
&filters[0][op]=contains
&filters[0][val]=laptop
&filters[1][fields]=Price
&filters[1][op]=gte
&filters[1][val]=500
&filters[2][fields]=Category
&filters[2][op]=eq
&filters[2][val]=Electronics
&sortings[0][prop]=Price
&sortings[0][ord]=desc
&sortings[1][prop]=Name
&sortings[1][ord]=asc
```

This query string represents:
- Page 1 (skip 0, take 20)
- 3 Filters (AND logic):
  - Name OR Description contains "laptop"
  - Price >= 500
  - Category equals "Electronics"
- 2 Sorts:
  - Primary: Price descending
  - Secondary: Name ascending

## Working with URLs

### Building URLs Programmatically

```csharp
public static class UrlBuilder {
    public static string BuildSearchUrl(string baseUrl, SearchParameters searchParams) {
        var dict = searchParams.ToDictionary();
        var queryString = string.Join("&", dict.Select(kvp => 
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"
        ));
        
        return $"{baseUrl}?{queryString}";
    }
}

// Usage:
var searchParams = new SearchParameters(Paging.FromPaging(0, 20))
    .AddFilters(new Filter("Name", FilterOperator.Contains, "laptop"));

var url = UrlBuilder.BuildSearchUrl("/api/products", searchParams);
// Result: /api/products?skip=0&take=20&filters[0][fields]=Name&filters[0][op]=contains&filters[0][val]=laptop
```

### Parsing URLs

```csharp
// In ASP.NET Core
[HttpGet]
public async Task<IActionResult> GetProducts([FromServices] PafisoSettings settings) {
    // Query string is automatically parsed into IQueryCollection
    var dict = Request.Query.ToDictionary(
        x => x.Key,
        x => x.Value.ToString()
    );
    
    var searchParams = SearchParameters.FromDictionary(dict);
    var result = await _dbContext.Products
        .WithSearchParameters(searchParams, settings)
        .ToPagedListAsync();
    
    return Ok(new { TotalCount = result.TotalEntries, Items = result.Entries });
}
```

### Using with HttpClient

```csharp
public class ProductApiClient {
    private readonly HttpClient _httpClient;
    
    public async Task<ProductSearchResult> SearchAsync(SearchParameters searchParams) {
        var dict = searchParams.ToDictionary();
        
        // Build query string
        var queryParams = new List<string>();
        foreach (var kvp in dict) {
            queryParams.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");
        }
        var queryString = string.Join("&", queryParams);
        
        // Make request
        var response = await _httpClient.GetAsync($"/api/products?{queryString}");
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<ProductSearchResult>();
    }
}
```

## Examples

### Example 1: Save and Load Search Criteria

```csharp
public class SavedSearchRepository {
    private readonly IDatabase _database;
    
    public async Task SaveSearchAsync(string userId, string name, SearchParameters searchParams) {
        var dict = searchParams.ToDictionary();
        var json = JsonSerializer.Serialize(dict);
        
        await _database.ExecuteAsync(
            "INSERT INTO SavedSearches (UserId, Name, Criteria) VALUES (@UserId, @Name, @Criteria)",
            new { UserId = userId, Name = name, Criteria = json }
        );
    }
    
    public async Task<SearchParameters?> LoadSearchAsync(string userId, string name) {
        var json = await _database.QueryFirstOrDefaultAsync<string>(
            "SELECT Criteria FROM SavedSearches WHERE UserId = @UserId AND Name = @Name",
            new { UserId = userId, Name = name }
        );
        
        if (json == null) return null;
        
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        return SearchParameters.FromDictionary(dict);
    }
}
```

### Example 2: URL-Friendly Search Links

```csharp
@model ProductListViewModel

<div class="search-filters">
    @foreach (var category in Model.Categories) {
        var searchParams = new SearchParameters(Paging.FromPaging(0, 20))
            .AddFilters(new Filter("Category", FilterOperator.Equals, category.Name));
        
        var dict = searchParams.ToDictionary();
        var queryString = string.Join("&", dict.Select(kvp => 
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"
        ));
        
        <a href="/products?@queryString">@category.Name</a>
    }
</div>
```

### Example 3: API Response with Next/Previous Page Links

```csharp
public class PaginatedResponse<T> {
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public string? NextPageUrl { get; set; }
    public string? PreviousPageUrl { get; set; }
}

[HttpGet]
public async Task<IActionResult> GetProducts([FromServices] PafisoSettings settings) {
    var searchParams = SearchParameters.FromDictionary(
        Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString())
    );
    
    var result = await _dbContext.Products
        .WithSearchParameters(searchParams, settings)
        .ToPagedListAsync();
    
    // Build next/previous page URLs
    string? nextPageUrl = null;
    string? prevPageUrl = null;
    
    if (searchParams.Paging != null) {
        var currentPage = searchParams.Paging.Page;
        var pageSize = searchParams.Paging.PageSize;
        var totalPages = (int)Math.Ceiling((double)result.TotalEntries / pageSize);
        
        if (currentPage < totalPages - 1) {
            var nextParams = new SearchParameters(Paging.FromPaging(currentPage + 1, pageSize)) {
                Filters = searchParams.Filters,
                Sortings = searchParams.Sortings
            };
            nextPageUrl = BuildUrl(Request.Path, nextParams);
        }
        
        if (currentPage > 0) {
            var prevParams = new SearchParameters(Paging.FromPaging(currentPage - 1, pageSize)) {
                Filters = searchParams.Filters,
                Sortings = searchParams.Sortings
            };
            prevPageUrl = BuildUrl(Request.Path, prevParams);
        }
    }
    
    return Ok(new PaginatedResponse<Product> {
        Items = result.Entries,
        TotalCount = result.TotalEntries,
        NextPageUrl = nextPageUrl,
        PreviousPageUrl = prevPageUrl
    });
}
```

### Example 4: Bookmarkable Searches

```csharp
public class SearchLinkGenerator {
    public static string GenerateSearchLink(
        string baseUrl,
        string? searchTerm = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? category = null,
        string? sortBy = null,
        bool sortDesc = false,
        int page = 0,
        int pageSize = 20) {
        
        var searchParams = new SearchParameters(Paging.FromPaging(page, pageSize));
        
        if (!string.IsNullOrEmpty(searchTerm)) {
            searchParams.AddFilters(
                new Filter(new[] { "Name", "Description" }, FilterOperator.Contains, searchTerm)
            );
        }
        
        if (minPrice.HasValue) {
            searchParams.AddFilters(
                new Filter("Price", FilterOperator.GreaterThanOrEquals, minPrice.Value.ToString())
            );
        }
        
        if (maxPrice.HasValue) {
            searchParams.AddFilters(
                new Filter("Price", FilterOperator.LessThanOrEquals, maxPrice.Value.ToString())
            );
        }
        
        if (!string.IsNullOrEmpty(category)) {
            searchParams.AddFilters(new Filter("Category", FilterOperator.Equals, category));
        }
        
        if (!string.IsNullOrEmpty(sortBy)) {
            var order = sortDesc ? SortOrder.Descending : SortOrder.Ascending;
            searchParams.AddSorting(new Sorting(sortBy, order));
        }
        
        var dict = searchParams.ToDictionary();
        var queryString = string.Join("&", dict.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"
        ));
        
        return $"{baseUrl}?{queryString}";
    }
}

// Usage:
var link = SearchLinkGenerator.GenerateSearchLink(
    "/products",
    searchTerm: "laptop",
    minPrice: 500,
    maxPrice: 2000,
    category: "Electronics",
    sortBy: "Price",
    sortDesc: true
);

// Can be shared, bookmarked, or used in emails
Console.WriteLine(link);
```

### Example 5: Deep Linking from External Sources

```csharp
// Mobile app or email link handler
public class DeepLinkHandler {
    public async Task<SearchResults> HandleProductSearchLinkAsync(Uri deepLink) {
        // Parse query string from deep link
        var query = HttpUtility.ParseQueryString(deepLink.Query);
        var dict = query.AllKeys.ToDictionary(k => k, k => query[k]);
        
        // Deserialize into SearchParameters
        var searchParams = SearchParameters.FromDictionary(dict);
        
        // Execute search
        return await ExecuteSearchAsync(searchParams);
    }
}

// Example deep link:
// myapp://products?skip=0&take=20&filters[0][fields]=Category&filters[0][op]=eq&filters[0][val]=Electronics
```

### Example 6: Query String Validation

```csharp
public class SearchParametersValidator {
    private const int MaxPageSize = 100;
    private const int MaxFilters = 10;
    private const int MaxSortings = 5;
    
    public static (bool isValid, string? error, SearchParameters? searchParams) 
        ValidateAndParse(IDictionary<string, string> queryString) {
        
        SearchParameters searchParams;
        try {
            searchParams = SearchParameters.FromDictionary(queryString);
        } catch (Exception ex) {
            return (false, $"Invalid query string format: {ex.Message}", null);
        }
        
        // Validate paging
        if (searchParams.Paging != null) {
            if (searchParams.Paging.Take > MaxPageSize) {
                return (false, $"Page size cannot exceed {MaxPageSize}", null);
            }
            
            if (searchParams.Paging.Skip < 0 || searchParams.Paging.Take < 1) {
                return (false, "Invalid paging parameters", null);
            }
        }
        
        // Validate filters
        if (searchParams.Filters.Count > MaxFilters) {
            return (false, $"Cannot apply more than {MaxFilters} filters", null);
        }
        
        // Validate sortings
        if (searchParams.Sortings.Count > MaxSortings) {
            return (false, $"Cannot apply more than {MaxSortings} sortings", null);
        }
        
        return (true, null, searchParams);
    }
}

// Usage in controller:
[HttpGet]
public async Task<IActionResult> GetProducts([FromServices] PafisoSettings settings) {
    var dict = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
    var (isValid, error, searchParams) = SearchParametersValidator.ValidateAndParse(dict);
    
    if (!isValid) {
        return BadRequest(new { Error = error });
    }
    
    var result = await _dbContext.Products
        .WithSearchParameters(searchParams, settings)
        .ToPagedListAsync();
    
    return Ok(new { TotalCount = result.TotalEntries, Items = result.Entries });
}
```

### Example 7: Search History Tracking

```csharp
public class SearchHistoryService {
    private readonly IDatabase _database;
    
    public async Task TrackSearchAsync(string userId, SearchParameters searchParams) {
        var dict = searchParams.ToDictionary();
        var json = JsonSerializer.Serialize(dict);
        
        await _database.ExecuteAsync(@"
            INSERT INTO SearchHistory (UserId, SearchCriteria, SearchDate)
            VALUES (@UserId, @Criteria, @Date)",
            new { UserId = userId, Criteria = json, Date = DateTime.UtcNow }
        );
    }
    
    public async Task<List<SearchParameters>> GetRecentSearchesAsync(string userId, int limit = 10) {
        var searches = await _database.QueryAsync<string>(@"
            SELECT TOP(@Limit) SearchCriteria
            FROM SearchHistory
            WHERE UserId = @UserId
            ORDER BY SearchDate DESC",
            new { UserId = userId, Limit = limit }
        );
        
        return searches
            .Select(json => {
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                return SearchParameters.FromDictionary(dict);
            })
            .ToList();
    }
}
```

## Best Practices

1. **URL Encode Values** - Always URL-encode dictionary values when building query strings
2. **Validate Input** - Validate query string parameters before deserialization
3. **Limit Complexity** - Set maximum limits on filters, sortings, and page sizes
4. **Handle Nulls** - Remember that `Paging.FromDictionary()` returns null if keys are missing
5. **Version Your Format** - If you change serialization format, consider versioning for backward compatibility
6. **Document Format** - Provide clear documentation of query string format for API consumers
7. **Test Edge Cases** - Test with special characters, empty values, and malformed input

## Format Specification

### Dictionary Keys

#### Paging
- `skip` - Number of items to skip (integer)
- `take` - Number of items to take (integer)

#### Filters (Indexed)
- `filters[{index}][fields]` - Comma-separated field names
- `filters[{index}][op]` - Operator code (eq, neq, gt, lt, gte, lte, contains, ncontains, null, notnull)
- `filters[{index}][val]` - Filter value (omitted for null/notnull operators)
- `filters[{index}][case]` - Case sensitivity flag ("true" if case-sensitive, omitted otherwise)

#### Sortings (Indexed)
- `sortings[{index}][prop]` - Property name to sort by
- `sortings[{index}][ord]` - Sort order (asc or desc)

## Next Steps

- Use [ASP.NET Core Integration](aspnetcore-integration.md) for automatic query string parsing
- Learn about [Configuration & Settings](configuration.md) for field name mapping
- Secure your API with [Field Restrictions](field-restrictions.md)
