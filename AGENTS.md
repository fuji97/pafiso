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

- **`SearchParameters`** - Combines Paging, Sorting, and Filter into a single object. Apply to queries via `ApplyToIQueryable<T>()`. Serializes to/from dictionary for query string support.

- **`Filter` / `Filter<T>`** - Represents a filter condition with field(s), operator, value, and case sensitivity. Multiple fields create OR conditions. Create from expressions via `Filter.FromExpression<T>(x => x.Age > 20)`.

- **`Sorting` / `Sorting<T>`** - Represents sort order for a property. Create from expressions via `Sorting.FromExpression<T>(x => x.Name, SortOrder.Ascending)`.

- **`Paging`** - Represents pagination as skip/take. Create via `Paging.FromPaging(page, pageSize)` or `Paging.FromSkipTake(skip, take)`.

### Result Types

- **`PagedQueryable<T>`** / **`PagedEnumerable<T>`** - Wrappers that include both the filtered/sorted/paged results and `TotalEntries` (count before paging).

### Extension Methods (in `Pafiso.Util`)

- `IQueryable<T>.Where(Filter)` - Apply filter to queryable
- `IQueryable<T>.OrderBy(Sorting)` / `ThenBy(Sorting)` - Apply sorting
- `IQueryable<T>.Paging(Paging)` - Apply pagination

### Key Dependencies

- **LinqKit** - Used for `PredicateBuilder` to compose OR predicates across multiple filter fields

## Testing

Tests use NUnit 4 with Shouldly for assertions. Test files mirror the core types: `FilterTest.cs`, `SortingTest.cs`, `PagingTest.cs`, `SearchParameterTest.cs`.
