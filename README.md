# Pafiso
A library to serialize, deserialize and apply Paging, Filtering and Sorting.

[![NuGet Version](https://img.shields.io/nuget/v/Pafiso.svg)](https://www.nuget.org/packages/Pafiso/)
![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/fuji97/pafiso/deploy-package.yml)

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

Parse query parameters from an HTTP request and apply them to a database query:

```csharp
[HttpGet]
public IActionResult GetProducts()
{
    // Convert query string to dictionary
    var queryDict = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());

    // Parse into SearchParameters
    var searchParams = SearchParameters.FromDictionary(queryDict);

    // Apply to your IQueryable (e.g., Entity Framework DbSet)
    PagedQueryable<Product> result = searchParams.ApplyToIQueryable(_dbContext.Products);

    return Ok(new {
        TotalCount = result.TotalEntries,
        Items = result.ToList()
    });
}
```

Example query string:
```
GET /products?skip=0&take=10&filters[0][fields]=Name&filters[0][op]=Contains&filters[0][val]=phone&sortings[0][prop]=Price&sortings[0][ord]=Descending
```

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

