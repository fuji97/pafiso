# Sorting

Sorting allows you to order your query results by one or more properties in ascending or descending order. Pafiso provides flexible sorting capabilities with support for multi-level sorting (ThenBy).

## Table of Contents

- [Creating Sorting](#creating-sorting)
  - [From Lambda Expressions](#from-lambda-expressions)
  - [Manual Creation](#manual-creation)
- [Sort Order](#sort-order)
- [Applying Sorting](#applying-sorting)
- [Multi-Level Sorting (ThenBy)](#multi-level-sorting-thenby)
- [Typed Sorting](#typed-sorting)
- [Serialization](#serialization)
- [Examples](#examples)

## Creating Sorting

### From Lambda Expressions

The recommended way to create sorting is using lambda expressions for type safety:

```csharp
using Pafiso;

// Ascending order
var sorting = Sorting.FromExpression<Product>(x => x.Name, SortOrder.Ascending);

// Descending order
var sorting = Sorting.FromExpression<Product>(x => x.Price, SortOrder.Descending);

// Sort by date
var sorting = Sorting.FromExpression<Product>(x => x.CreatedDate, SortOrder.Descending);
```

### Manual Creation

You can also create sorting manually using string-based property names:

```csharp
// Constructor with property name and order
var sorting = new Sorting("Name", SortOrder.Ascending);

// Descending by price
var sorting = new Sorting("Price", SortOrder.Descending);
```

## Sort Order

Pafiso supports two sort orders through the `SortOrder` enum:

| Enum Value | Serialization | Description |
|------------|---------------|-------------|
| `SortOrder.Ascending` | `asc` | Sort from lowest to highest (A-Z, 0-9, oldest to newest) |
| `SortOrder.Descending` | `desc` | Sort from highest to lowest (Z-A, 9-0, newest to oldest) |

```csharp
// Convenience properties
var sorting = new Sorting("Name", SortOrder.Ascending);
Console.WriteLine(sorting.Ascending);  // true
Console.WriteLine(sorting.Descending); // false
```

## Applying Sorting

### Using Extension Methods

The most convenient way to apply sorting is using extension methods:

```csharp
using Pafiso.Util;

var sorting = Sorting.FromExpression<Product>(x => x.Name, SortOrder.Ascending);
var orderedProducts = products.OrderBy(sorting).ToList();
```

### Using ApplyToIQueryable Method

You can also use the method directly:

```csharp
var sorting = Sorting.FromExpression<Product>(x => x.Price, SortOrder.Descending);
IOrderedQueryable<Product> ordered = sorting.ApplyToIQueryable(products);
var results = ordered.ToList();
```

### With Settings

Apply sorting with custom settings for field name resolution:

```csharp
var settings = new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

// Field name "productName" will be resolved to "ProductName"
var sorting = new Sorting("productName", SortOrder.Ascending);
var results = sorting.ApplyToIQueryable(products, settings).ToList();
```

### With Field Restrictions

Apply sorting with security restrictions:

```csharp
var restrictions = new FieldRestrictions()
    .AllowSorting<Product>(x => x.Name, x => x.Price, x => x.CreatedDate)
    .BlockSorting<Product>(x => x.InternalCost);

var sorting = new Sorting("Name", SortOrder.Ascending);
var ordered = sorting.ApplyToIQueryable(products, restrictions);

// If sorting is blocked, returns null
if (ordered != null) {
    var results = ordered.ToList();
}
```

## Multi-Level Sorting (ThenBy)

Pafiso supports multi-level sorting for complex ordering requirements:

### Using Extension Methods

```csharp
using Pafiso.Util;

var primarySort = Sorting.FromExpression<Product>(x => x.Category, SortOrder.Ascending);
var secondarySort = Sorting.FromExpression<Product>(x => x.Price, SortOrder.Descending);
var tertiarySort = Sorting.FromExpression<Product>(x => x.Name, SortOrder.Ascending);

// Chain multiple sorts
var results = products
    .OrderBy(primarySort)
    .ThenBy(secondarySort)
    .ThenBy(tertiarySort)
    .ToList();
```

### Using ThenApplyToIQueryable Method

```csharp
var primarySort = Sorting.FromExpression<Product>(x => x.Category, SortOrder.Ascending);
var secondarySort = Sorting.FromExpression<Product>(x => x.Price, SortOrder.Descending);

IOrderedQueryable<Product> ordered = primarySort.ApplyToIQueryable(products);
ordered = secondarySort.ThenApplyToIQueryable(ordered);

var results = ordered.ToList();
```

### With List of Sorting

```csharp
List<Sorting> sortings = new() {
    Sorting.FromExpression<Product>(x => x.Category, SortOrder.Ascending),
    Sorting.FromExpression<Product>(x => x.Price, SortOrder.Descending),
    Sorting.FromExpression<Product>(x => x.Name, SortOrder.Ascending)
};

IQueryable<Product> query = products;

// Apply first sort
if (sortings.Count > 0) {
    var ordered = sortings[0].ApplyToIQueryable(query);
    
    // Apply remaining sorts
    for (int i = 1; i < sortings.Count; i++) {
        ordered = sortings[i].ThenApplyToIQueryable(ordered);
    }
    
    var results = ordered.ToList();
}
```

### ThenBy with Restrictions

When using restrictions with ThenBy, blocked fields are silently ignored (the query is returned unchanged):

```csharp
var restrictions = new FieldRestrictions()
    .AllowSorting<Product>(x => x.Name, x => x.Price);

var primarySort = new Sorting("Name", SortOrder.Ascending);
var secondarySort = new Sorting("InternalCost", SortOrder.Descending); // Blocked!

var ordered = primarySort.ApplyToIQueryable(products, restrictions);
if (ordered != null) {
    // secondarySort is silently ignored since InternalCost is not allowed
    ordered = secondarySort.ThenApplyToIQueryable(ordered, restrictions);
    var results = ordered.ToList();
}
```

## Typed Sorting

Pafiso provides a generic `Sorting<T>` class for stronger type association:

```csharp
// Create a typed sorting
Sorting<Product> sorting = Sorting.FromExpression<Product>(
    x => x.Name, 
    SortOrder.Ascending
);

// The type is preserved
Sorting<Product> typedSorting = new Sorting<Product>("Price", SortOrder.Descending);
```

## Serialization

Sorting can be serialized to and from dictionaries for query string support:

### To Dictionary

```csharp
var sorting = new Sorting("Price", SortOrder.Descending);
IDictionary<string, string> dict = sorting.ToDictionary();

// Results in:
// {
//   "prop": "Price",
//   "ord": "desc"
// }
```

### From Dictionary

```csharp
var dict = new Dictionary<string, string> {
    ["prop"] = "Name",
    ["ord"] = "asc"
};

var sorting = Sorting.FromDictionary(dict);
```

## Examples

### Example 1: Simple Product Sorting

```csharp
// Sort products by price (highest to lowest)
public IQueryable<Product> GetProductsByPrice(IQueryable<Product> products) {
    var sorting = Sorting.FromExpression<Product>(x => x.Price, SortOrder.Descending);
    return products.OrderBy(sorting);
}

// Usage
var expensiveFirst = GetProductsByPrice(dbContext.Products).ToList();
```

### Example 2: Multi-Level Sorting

```csharp
// Sort by category, then by price within each category
public IQueryable<Product> GetCategorizedProducts(IQueryable<Product> products) {
    var categorySort = Sorting.FromExpression<Product>(x => x.Category, SortOrder.Ascending);
    var priceSort = Sorting.FromExpression<Product>(x => x.Price, SortOrder.Descending);
    
    return products
        .OrderBy(categorySort)
        .ThenBy(priceSort);
}

// Usage
var categorized = GetCategorizedProducts(dbContext.Products).ToList();
// Results: Electronics (expensive first), Home (expensive first), etc.
```

### Example 3: Dynamic Sorting

```csharp
// Sort based on user-selected field and order
public IQueryable<Product> SortProducts(
    string sortField,
    string sortOrder,
    IQueryable<Product> products) {
    
    var order = sortOrder.ToLower() == "desc" 
        ? SortOrder.Descending 
        : SortOrder.Ascending;
    
    var sorting = sortField.ToLower() switch {
        "name" => new Sorting("Name", order),
        "price" => new Sorting("Price", order),
        "category" => new Sorting("Category", order),
        "date" => new Sorting("CreatedDate", order),
        _ => new Sorting("Name", SortOrder.Ascending) // Default
    };
    
    return products.OrderBy(sorting);
}

// Usage
var sorted = SortProducts("price", "desc", dbContext.Products).ToList();
```

### Example 4: Conditional Sorting

```csharp
// Apply different sorting based on user preferences
public class SortPreferences {
    public bool SortByPopularity { get; set; }
    public bool SortByPrice { get; set; }
    public bool SortByNewest { get; set; }
}

public IQueryable<Product> GetSortedProducts(
    SortPreferences prefs,
    IQueryable<Product> products) {
    
    if (prefs.SortByPopularity) {
        var sorting = Sorting.FromExpression<Product>(
            x => x.SalesCount, 
            SortOrder.Descending
        );
        return products.OrderBy(sorting);
    }
    
    if (prefs.SortByPrice) {
        var sorting = Sorting.FromExpression<Product>(
            x => x.Price, 
            SortOrder.Ascending
        );
        return products.OrderBy(sorting);
    }
    
    if (prefs.SortByNewest) {
        var sorting = Sorting.FromExpression<Product>(
            x => x.CreatedDate, 
            SortOrder.Descending
        );
        return products.OrderBy(sorting);
    }
    
    // Default sorting
    var defaultSort = Sorting.FromExpression<Product>(
        x => x.Name, 
        SortOrder.Ascending
    );
    return products.OrderBy(defaultSort);
}
```

### Example 5: Complex Multi-Level Sorting with Status Priority

```csharp
// Sort with custom status priority, then by date
public enum ProductStatus {
    Featured = 1,
    Active = 2,
    LowStock = 3,
    OutOfStock = 4,
    Discontinued = 5
}

public IQueryable<Product> GetPrioritizedProducts(IQueryable<Product> products) {
    // First sort: Status priority (Featured first)
    var statusSort = Sorting.FromExpression<Product>(
        x => x.Status, 
        SortOrder.Ascending
    );
    
    // Second sort: Within same status, show newest first
    var dateSort = Sorting.FromExpression<Product>(
        x => x.CreatedDate, 
        SortOrder.Descending
    );
    
    // Third sort: If same status and date, sort by name
    var nameSort = Sorting.FromExpression<Product>(
        x => x.Name, 
        SortOrder.Ascending
    );
    
    return products
        .OrderBy(statusSort)
        .ThenBy(dateSort)
        .ThenBy(nameSort);
}
```

### Example 6: Sorting with Security Restrictions

```csharp
// Allow users to sort only by specific fields
[HttpGet]
public async Task<IActionResult> GetProducts(
    [FromQuery] string? sortBy, 
    [FromQuery] string? order,
    [FromServices] PafisoSettings settings) {
    
    var restrictions = new FieldRestrictions()
        .AllowSorting<Product>(
            x => x.Name,
            x => x.Price,
            x => x.CreatedDate,
            x => x.Rating
        );
    
    var searchParams = new SearchParameters();
    
    if (!string.IsNullOrEmpty(sortBy)) {
        var sortOrder = order?.ToLower() == "desc" 
            ? SortOrder.Descending 
            : SortOrder.Ascending;
        
        searchParams.AddSorting(new Sorting(sortBy, sortOrder));
    }
    
    var result = await _dbContext.Products
        .WithSearchParameters(searchParams, settings, restrictions)
        .ToPagedListAsync();
    
    return Ok(result.Entries);
}
```

### Example 7: Building Sort List from Query Parameters

```csharp
// Support multiple sort parameters like: ?sort=category:asc,price:desc,name:asc
public IQueryable<Product> ApplySorting(
    string? sortParam,
    IQueryable<Product> products) {
    
    if (string.IsNullOrEmpty(sortParam)) {
        // Default sort
        return products.OrderBy(
            Sorting.FromExpression<Product>(x => x.Name, SortOrder.Ascending)
        );
    }
    
    var sortings = new List<Sorting>();
    var sortSpecs = sortParam.Split(',');
    
    foreach (var spec in sortSpecs) {
        var parts = spec.Split(':');
        if (parts.Length != 2) continue;
        
        var field = parts[0].Trim();
        var order = parts[1].Trim().ToLower() == "desc" 
            ? SortOrder.Descending 
            : SortOrder.Ascending;
        
        sortings.Add(new Sorting(field, order));
    }
    
    if (sortings.Count == 0) {
        return products;
    }
    
    // Apply first sort
    var query = products.OrderBy(sortings[0]);
    
    // Apply subsequent sorts
    for (int i = 1; i < sortings.Count; i++) {
        query = query.ThenBy(sortings[i]);
    }
    
    return query;
}

// Usage
var sorted = ApplySorting("category:asc,price:desc,name:asc", dbContext.Products);
```

## Best Practices

1. **Always provide a default sort** - Ensure predictable results by always having at least one sorting criterion
2. **Use multi-level sorting for ties** - Add secondary sorts to break ties in the primary sort
3. **Consider performance** - Sorting can be expensive; add database indexes on frequently sorted columns
4. **Validate sort fields** - Use field restrictions to prevent sorting on sensitive or complex computed fields
5. **Type safety** - Prefer expression-based sorting over string-based when possible

## Next Steps

- Combine sorting with [Filtering](filtering.md) for refined queries
- Learn about [Paging](paging.md) to limit result sets
- Use [SearchParameters](search-parameters.md) to combine sorting, filtering, and paging
- Secure sorting with [Field Restrictions](field-restrictions.md)
