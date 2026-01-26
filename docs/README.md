# Pafiso Documentation

Welcome to the Pafiso documentation! This guide will help you understand and use Pafiso effectively in your .NET applications.

## What is Pafiso?

Pafiso is a .NET library that provides a comprehensive solution for **Pa**ging, **Fi**ltering, and **So**rting operations on `IQueryable<T>` and `IEnumerable<T>` collections. It's designed to make building dynamic queries simple and type-safe, with built-in support for serialization to query strings and seamless integration with ASP.NET Core and Entity Framework Core.

## Key Features

- **Type-safe filtering** - Build filters from lambda expressions with compile-time validation
- **Flexible sorting** - Single or multi-field sorting with ascending/descending order
- **Efficient paging** - Support for both skip/take and page-based pagination
- **Query string serialization** - Serialize and deserialize search parameters to/from dictionaries
- **ASP.NET Core integration** - Directly parse query strings into search parameters
- **EF Core optimization** - Optimized case-insensitive filtering using SQL LIKE
- **Field restrictions** - Secure your API by controlling which fields can be filtered/sorted
- **Customizable configuration** - Control field name mapping, case sensitivity, and more

## Getting Started

### Installation

Install Pafiso via NuGet:

```bash
dotnet add package Pafiso
```

For ASP.NET Core integration:

```bash
dotnet add package Pafiso.AspNetCore
```

For Entity Framework Core optimization:

```bash
dotnet add package Pafiso.EntityFrameworkCore
```

### Quick Example

```csharp
using Pafiso;
using Pafiso.EntityFrameworkCore.Enumerables;

// Create a filter
var filter = Filter.FromExpression<Product>(x => x.Price > 100);

// Create sorting
var sorting = Sorting.FromExpression<Product>(x => x.Name, SortOrder.Ascending);

// Create paging
var paging = Paging.FromPaging(page: 0, pageSize: 20);

// Combine into SearchParameters
var searchParams = new SearchParameters(paging)
    .AddFilters(filter)
    .AddSorting(sorting);

// Apply to your query
var result = await dbContext.Products
    .WithSearchParameters(searchParams)
    .ToPagedListAsync();

Console.WriteLine($"Total: {result.TotalEntries}");
foreach (var product in result.Entries) {
    Console.WriteLine($"{product.Name}: ${product.Price}");
}
```

## Documentation Index

### Core Concepts

- **[Filtering](filtering.md)** - Learn how to create and apply filters with various operators
- **[Sorting](sorting.md)** - Sort data by single or multiple fields
- **[Paging](paging.md)** - Implement pagination in your queries
- **[SearchParameters](search-parameters.md)** - Combine filtering, sorting, and paging

### Serialization & Integration

- **[Serialization](serialization.md)** - Serialize to/from dictionaries and query strings
- **[ASP.NET Core Integration](aspnetcore-integration.md)** - Use Pafiso in your web APIs
- **[Entity Framework Core Integration](efcore-integration.md)** - Optimize for EF Core

### Configuration & Security

- **[Configuration & Settings](configuration.md)** - Customize field name mapping and behavior
- **[Field Restrictions](field-restrictions.md)** - Control which fields can be filtered/sorted

### Advanced Topics

- **[Advanced Scenarios](advanced-scenarios.md)** - Complex queries, custom operators, and more

## Package Structure

Pafiso is distributed as three NuGet packages:

- **Pafiso** - Core library with filtering, sorting, and paging functionality
- **Pafiso.AspNetCore** - ASP.NET Core integration (dependency injection, query string parsing)
- **Pafiso.EntityFrameworkCore** - EF Core optimizations (case-insensitive LIKE operations)

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                   SearchParameters                       │
│  (Combines Paging + Filters + Sorting)                  │
└────────────┬────────────────────────────────────────────┘
             │
             ├─► Paging (Skip/Take)
             │
             ├─► Filters (List<Filter>)
             │   └─► Filter (Fields, Operator, Value, CaseSensitive)
             │
             └─► Sorting (List<Sorting>)
                 └─► Sorting (Property, Order)

┌─────────────────────────────────────────────────────────┐
│                   Extension Methods                      │
│  .Where() .OrderBy() .ThenBy() .Paging()                │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                   Result Types                           │
│  PagedQueryable<T>, PagedEnumerable<T>, PagedList<T>    │
│  (Include TotalEntries count)                           │
└─────────────────────────────────────────────────────────┘
```

## Support

- **Issues**: [GitHub Issues](https://github.com/fuji97/pafiso/issues)
- **Discussions**: [GitHub Discussions](https://github.com/fuji97/pafiso/discussions)

## License

Pafiso is licensed under the MIT License. See the [LICENSE](../LICENSE) file for details.
