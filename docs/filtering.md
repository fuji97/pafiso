# Filtering

Filtering is one of the core features of Pafiso, allowing you to apply dynamic conditions to your `IQueryable<T>` or `IEnumerable<T>` collections.

## Table of Contents

- [Creating Filters](#creating-filters)
  - [From Lambda Expressions](#from-lambda-expressions)
  - [Manual Creation](#manual-creation)
- [Filter Operators](#filter-operators)
- [Multi-Field Filtering (OR Conditions)](#multi-field-filtering-or-conditions)
- [Case Sensitivity](#case-sensitivity)
- [Applying Filters](#applying-filters)
- [Typed Filters](#typed-filters)
- [Serialization](#serialization)
- [Examples](#examples)

## Creating Filters

### From Lambda Expressions

The recommended way to create filters is using lambda expressions, which provides type safety and compile-time validation:

```csharp
using Pafiso;

// Simple equality
var filter = Filter.FromExpression<Product>(x => x.Category == "Electronics");

// Greater than
var filter = Filter.FromExpression<Product>(x => x.Price > 100);

// Less than or equal
var filter = Filter.FromExpression<Product>(x => x.Stock <= 10);

// String contains
var filter = Filter.FromExpression<Product>(x => x.Name.Contains("iPhone"));

// Null check
var filter = Filter.FromExpression<Product>(x => x.Description == null);

// Not null check
var filter = Filter.FromExpression<Product>(x => x.Description != null);
```

### Manual Creation

You can also create filters manually using string-based field names:

```csharp
// Basic constructor
var filter = new Filter("Price", FilterOperator.GreaterThan, "100");

// With case sensitivity
var filter = new Filter("Name", FilterOperator.Contains, "phone", caseSensitive: true);

// Multiple fields (OR condition)
var filter = new Filter(
    fields: new[] { "Name", "Description" },
    @operator: FilterOperator.Contains,
    value: "phone"
);
```

## Filter Operators

Pafiso supports the following filter operators:

| Operator | Enum Value | Description | Example |
|----------|-----------|-------------|---------|
| `Equals` | `eq` | Exact match | `x => x.Status == "Active"` |
| `NotEquals` | `neq` | Not equal to | `x => x.Status != "Deleted"` |
| `GreaterThan` | `gt` | Greater than | `x => x.Price > 100` |
| `LessThan` | `lt` | Less than | `x => x.Stock < 10` |
| `GreaterThanOrEquals` | `gte` | Greater than or equal | `x => x.Price >= 50` |
| `LessThanOrEquals` | `lte` | Less than or equal | `x => x.Stock <= 100` |
| `Contains` | `contains` | String contains (case-insensitive by default) | `x => x.Name.Contains("phone")` |
| `NotContains` | `ncontains` | String does not contain | `x => !x.Name.Contains("refurbished")` |
| `Null` | `null` | Field is null | `x => x.Description == null` |
| `NotNull` | `notnull` | Field is not null | `x => x.Description != null` |

### Operator Examples

```csharp
// Equality
var active = Filter.FromExpression<Product>(x => x.IsActive == true);
var inactive = Filter.FromExpression<Product>(x => x.IsActive != true);

// Numeric comparisons
var expensive = Filter.FromExpression<Product>(x => x.Price > 1000);
var affordable = Filter.FromExpression<Product>(x => x.Price <= 50);

// String operations
var search = Filter.FromExpression<Product>(x => x.Name.Contains("laptop"));
var exclude = Filter.FromExpression<Product>(x => !x.Description.Contains("discontinued"));

// Null checks
var hasDescription = Filter.FromExpression<Product>(x => x.Description != null);
var noImage = Filter.FromExpression<Product>(x => x.ImageUrl == null);
```

## Multi-Field Filtering (OR Conditions)

You can filter across multiple fields with OR logic. This is useful for search functionality where you want to match any of several fields:

```csharp
// Search in both Name and Description
var searchFilter = Filter.FromExpression<Product>(x => x.Name.Contains("laptop"))
    .AddField<Product>(x => x.Description);

// Manually specify multiple fields
var filter = new Filter(
    fields: new[] { "Name", "Description", "Category" },
    @operator: FilterOperator.Contains,
    value: "computer"
);

// Apply the filter - matches products where ANY field contains "computer"
var results = products.Where(searchFilter);
```

**Important**: Multiple fields in a single filter create OR conditions. If you need AND conditions, create separate filters and apply them sequentially or use `SearchParameters`.

```csharp
// OR condition: Name OR Description contains "laptop"
var orFilter = Filter.FromExpression<Product>(x => x.Name.Contains("laptop"))
    .AddField<Product>(x => x.Description);

// AND condition: Name contains "laptop" AND Price > 500
var nameFilter = Filter.FromExpression<Product>(x => x.Name.Contains("laptop"));
var priceFilter = Filter.FromExpression<Product>(x => x.Price > 500);
var andResults = products.Where(nameFilter).Where(priceFilter);
```

## Case Sensitivity

By default, string operations (Contains, Equals) are case-insensitive. You can control this behavior:

### Expression-Based Filters

For expression-based filters, case sensitivity is determined by the global `PafisoSettings`:

```csharp
// Configure global case sensitivity
PafisoSettings.Default = new PafisoSettings {
    StringComparison = StringComparison.Ordinal // Case-sensitive
};

var filter = Filter.FromExpression<Product>(x => x.Name.Contains("iPhone"));
```

### Manual Filters

For manually created filters, specify case sensitivity in the constructor:

```csharp
// Case-insensitive (default)
var caseInsensitive = new Filter("Name", FilterOperator.Contains, "iphone");

// Case-sensitive
var caseSensitive = new Filter("Name", FilterOperator.Contains, "iPhone", caseSensitive: true);
```

## Applying Filters

### Using Extension Methods

The simplest way to apply filters is using the `.Where()` extension method:

```csharp
using Pafiso.Util;

var filter = Filter.FromExpression<Product>(x => x.Price > 100);
var expensiveProducts = products.Where(filter).ToList();
```

### Using ApplyFilter Method

You can also use the `ApplyFilter` method directly:

```csharp
var filter = Filter.FromExpression<Product>(x => x.Category == "Electronics");
var electronics = filter.ApplyFilter(products).ToList();
```

### With Settings

Apply filters with custom settings:

```csharp
var settings = new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    StringComparison = StringComparison.OrdinalIgnoreCase
};

var filter = new Filter("categoryName", FilterOperator.Equals, "Electronics");
var results = filter.ApplyFilter(products, settings).ToList();
```

### With Field Restrictions

Apply filters with security restrictions (see [Field Restrictions](field-restrictions.md) for details):

```csharp
var restrictions = new FieldRestrictions()
    .AllowFiltering<Product>(x => x.Name, x => x.Price, x => x.Category)
    .BlockFiltering<Product>(x => x.InternalCost);

var filter = new Filter("Name", FilterOperator.Contains, "laptop");
var results = filter.ApplyFilter(products, restrictions).ToList();
```

## Typed Filters

Pafiso provides a generic `Filter<T>` class for stronger type association:

```csharp
// Create a typed filter
Filter<Product> filter = Filter.FromExpression<Product>(x => x.Price > 100);

// Add fields in a type-safe manner
filter.AddField(x => x.Name);

// The return type is Filter<Product>, not just Filter
```

The main benefit of `Filter<T>` is that the `AddField()` method returns `Filter<T>` instead of `Filter`, allowing for better type inference in some scenarios.

## Serialization

Filters can be serialized to and from dictionaries, which is useful for query strings:

### To Dictionary

```csharp
var filter = new Filter("Name", FilterOperator.Contains, "phone");
IDictionary<string, string> dict = filter.ToDictionary();

// Results in:
// {
//   "fields": "Name",
//   "op": "contains",
//   "val": "phone"
// }

// With multiple fields
var multiFilter = new Filter(
    new[] { "Name", "Description" },
    FilterOperator.Contains,
    "phone"
);
var dict = multiFilter.ToDictionary();
// {
//   "fields": "Name,Description",
//   "op": "contains",
//   "val": "phone"
// }

// With case sensitivity
var caseSensitiveFilter = new Filter("Name", FilterOperator.Equals, "iPhone", caseSensitive: true);
var dict = caseSensitiveFilter.ToDictionary();
// {
//   "fields": "Name",
//   "op": "eq",
//   "val": "iPhone",
//   "case": "true"
// }
```

### From Dictionary

```csharp
var dict = new Dictionary<string, string> {
    ["fields"] = "Price",
    ["op"] = "gt",
    ["val"] = "100"
};

var filter = Filter.FromDictionary(dict);
```

## Examples

### Example 1: Product Search

```csharp
// Search products by name or description
public IQueryable<Product> SearchProducts(string searchTerm, IQueryable<Product> products) {
    var searchFilter = Filter.FromExpression<Product>(x => x.Name.Contains(searchTerm))
        .AddField<Product>(x => x.Description);
    
    return products.Where(searchFilter);
}

// Usage
var results = SearchProducts("laptop", dbContext.Products).ToList();
```

### Example 2: Price Range Filter

```csharp
// Filter products within a price range
public IQueryable<Product> FilterByPriceRange(
    decimal minPrice,
    decimal maxPrice,
    IQueryable<Product> products) {
    
    var minFilter = Filter.FromExpression<Product>(x => x.Price >= minPrice);
    var maxFilter = Filter.FromExpression<Product>(x => x.Price <= maxPrice);
    
    return products.Where(minFilter).Where(maxFilter);
}

// Usage
var affordableProducts = FilterByPriceRange(50, 500, dbContext.Products).ToList();
```

### Example 3: Category Filter with Null Handling

```csharp
// Filter products by category, handling null values
public IQueryable<Product> FilterByCategory(
    string? category,
    bool includeUncategorized,
    IQueryable<Product> products) {
    
    if (category == null) {
        return includeUncategorized 
            ? products 
            : products.Where(Filter.FromExpression<Product>(x => x.Category != null));
    }
    
    var categoryFilter = Filter.FromExpression<Product>(x => x.Category == category);
    return products.Where(categoryFilter);
}
```

### Example 4: Advanced Search with Multiple Conditions

```csharp
// Combine multiple filters for advanced search
public class ProductSearchCriteria {
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? InStock { get; set; }
}

public IQueryable<Product> AdvancedSearch(
    ProductSearchCriteria criteria,
    IQueryable<Product> products) {
    
    var query = products;
    
    // Text search
    if (!string.IsNullOrEmpty(criteria.SearchTerm)) {
        var searchFilter = Filter.FromExpression<Product>(x => x.Name.Contains(criteria.SearchTerm))
            .AddField<Product>(x => x.Description);
        query = query.Where(searchFilter);
    }
    
    // Category filter
    if (!string.IsNullOrEmpty(criteria.Category)) {
        var categoryFilter = Filter.FromExpression<Product>(x => x.Category == criteria.Category);
        query = query.Where(categoryFilter);
    }
    
    // Price range
    if (criteria.MinPrice.HasValue) {
        var minPriceFilter = Filter.FromExpression<Product>(x => x.Price >= criteria.MinPrice.Value);
        query = query.Where(minPriceFilter);
    }
    
    if (criteria.MaxPrice.HasValue) {
        var maxPriceFilter = Filter.FromExpression<Product>(x => x.Price <= criteria.MaxPrice.Value);
        query = query.Where(maxPriceFilter);
    }
    
    // Stock availability
    if (criteria.InStock.HasValue) {
        var stockFilter = criteria.InStock.Value
            ? Filter.FromExpression<Product>(x => x.Stock > 0)
            : Filter.FromExpression<Product>(x => x.Stock == 0);
        query = query.Where(stockFilter);
    }
    
    return query;
}
```

### Example 5: Dynamic Filtering from User Input

```csharp
// Build filters dynamically based on user input
public IQueryable<Product> DynamicFilter(
    Dictionary<string, string> filterParams,
    IQueryable<Product> products) {
    
    var query = products;
    
    foreach (var (field, value) in filterParams) {
        Filter filter = field.ToLower() switch {
            "name" => new Filter("Name", FilterOperator.Contains, value),
            "category" => new Filter("Category", FilterOperator.Equals, value),
            "minprice" => new Filter("Price", FilterOperator.GreaterThanOrEquals, value),
            "maxprice" => new Filter("Price", FilterOperator.LessThanOrEquals, value),
            _ => throw new ArgumentException($"Unknown filter field: {field}")
        };
        
        query = query.Where(filter);
    }
    
    return query;
}

// Usage
var filters = new Dictionary<string, string> {
    ["name"] = "laptop",
    ["minprice"] = "500",
    ["maxprice"] = "2000"
};
var results = DynamicFilter(filters, dbContext.Products).ToList();
```

## Next Steps

- Learn about [Sorting](sorting.md) to order your filtered results
- Combine filters with paging using [SearchParameters](search-parameters.md)
- Secure your API with [Field Restrictions](field-restrictions.md)
- Configure field name mapping in [Configuration & Settings](configuration.md)
