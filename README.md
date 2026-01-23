# Pafiso
A library to serialize, deserialize and apply Paging, Filtering and Sorting.

[![NuGet Version](https://img.shields.io/nuget/v/Pafiso.svg)](https://www.nuget.org/packages/Pafiso/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Pafiso.svg)](https://www.nuget.org/packages/Pafiso/)
![Build Status](https://img.shields.io/github/actions/workflow/status/fuji97/pafiso/test.yml)
![Deploy Status](https://img.shields.io/github/actions/workflow/status/fuji97/pafiso/deploy-package.yml)

## Installation
Install Pafiso via NuGet Package Manager:
```
PM> Install-Package Pafiso
```
Or via the .NET CLI:
```
dotnet add package Pafiso
```

## Usage

### Filtering

Create filters from lambda expressions or manually:

```csharp
// From expression
var filter = Filter.FromExpression<Product>(x => x.Price > 100);

// Manual creation
var filter = new Filter("Price", FilterOperator.GreaterThan, "100");

// Apply to a query
var results = products.Where(filter);
```

**Supported operators:** `Equals`, `NotEquals`, `GreaterThan`, `LessThan`, `GreaterThanOrEquals`, `LessThanOrEquals`, `Contains`, `NotContains`, `Null`, `NotNull`

Filter across multiple fields (OR condition):

```csharp
var filter = Filter.FromExpression<Product>(x => x.Name.Contains("phone"))
    .AddField(x => x.Description);

// Matches where Name OR Description contains "phone"
var results = products.Where(filter);
```

### Sorting

```csharp
// From expression
var sorting = Sorting.FromExpression<Product>(x => x.Name, SortOrder.Ascending);

// Manual creation
var sorting = new Sorting("Name", SortOrder.Ascending);

// Apply to a query
var ordered = products.OrderBy(sorting);

// Multiple sort criteria
var ordered = products.OrderBy(firstSorting).ThenBy(secondSorting);
```

### Paging

```csharp
// From page number and size (0-indexed)
var paging = Paging.FromPaging(page: 0, pageSize: 10);

// From skip/take
var paging = Paging.FromSkipTake(skip: 0, take: 10);

// Apply to a query
var pagedResults = products.Paging(paging);
```

### SearchParameters

Combine filtering, sorting, and paging into a single object:

```csharp
var searchParams = new SearchParameters(Paging.FromPaging(0, 10))
    .AddFilters(Filter.FromExpression<Product>(x => x.Price > 50))
    .AddSorting(Sorting.FromExpression<Product>(x => x.Name, SortOrder.Ascending));

// Apply all at once - returns PagedQueryable<T> with TotalEntries
PagedQueryable<Product> result = searchParams.ApplyToIQueryable(products);

Console.WriteLine($"Total: {result.TotalEntries}");
foreach (var product in result) {
    Console.WriteLine(product.Name);
}
```

### Serialization

Serialize to dictionary (useful for query strings):

```csharp
var searchParams = new SearchParameters(Paging.FromPaging(0, 10))
    .AddFilters(new Filter("Name", FilterOperator.Contains, "phone"))
    .AddSorting(new Sorting("Price", SortOrder.Descending));

IDictionary<string, string> dict = searchParams.ToDictionary();
// Results in: skip=0, take=10, filters[0][fields]=Name, filters[0][op]=Contains, filters[0][val]=phone, ...

// Deserialize back
var restored = SearchParameters.FromDictionary(dict);
```

### ASP.NET Core Integration

Install the ASP.NET Core integration package:
```
PM> Install-Package Pafiso.AspNetCore
```
Or via the .NET CLI:
```
dotnet add package Pafiso.AspNetCore
```

Parse query parameters from an HTTP request and apply them to a database query:

```csharp
using Pafiso.AspNetCore;

[HttpGet]
public IActionResult GetProducts()
{
    // Convert query string directly to SearchParameters
    var searchParams = Request.Query.ToSearchParameters();

    // Apply to your IQueryable (e.g., Entity Framework DbSet)
    var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(_dbContext.Products);

    return Ok(new {
        TotalCount = countQuery.Count(),
        Items = pagedQuery.ToList()
    });
}
```

Example query string:
```
GET /products?skip=0&take=10&filters[0][fields]=Name&filters[0][op]=Contains&filters[0][val]=phone&sortings[0][prop]=Price&sortings[0][ord]=Descending
```

### Field Restrictions

Control which fields can be filtered and sorted by clients using `FieldRestrictions`:

```csharp
[HttpGet]
public IActionResult GetProducts()
{
    var searchParams = Request.Query.ToSearchParameters();

    // Apply with field restrictions
    var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(
        _dbContext.Products,
        restrictions => restrictions
            .AllowFiltering<Product>(x => x.Name, x => x.Price, x => x.Category)
            .AllowSorting<Product>(x => x.Name, x => x.Price)
            .BlockFiltering<Product>(x => x.InternalCost)
    );

    return Ok(new {
        TotalCount = countQuery.Count(),
        Items = pagedQuery.ToList()
    });
}
```

Restriction methods:
- `AllowFiltering` / `AllowSorting` - Allowlist specific fields (all others are blocked)
- `BlockFiltering` / `BlockSorting` - Blocklist specific fields (all others are allowed)
- Blocklist takes precedence over allowlist
- Supports both expression-based (`x => x.Name`) and string-based (`"Name"`) field specification

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

