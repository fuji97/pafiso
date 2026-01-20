# Pafiso (TypeScript)

TypeScript library for generating Paging, Filtering, and Sorting parameters for REST requests. Compatible with the Pafiso .NET library.

> **⚠️ WARNING: Proof-of-Concept Library**
> 
> This library was generated as a proof-of-concept using AI code generation (vibecoding) and it has NOT been tested. **Use with caution** and thoroughly test in your specific use case. Contributions, bug reports, and improvements are welcome!

## Installation

```bash
npm install pafiso
```

## Quick Start

```typescript
import { search, Sorting, FilterOperator } from 'pafiso';

// Build search parameters with fluent API
const params = search()
  .filter(f => f.field('Name').contains('Franco'))
  .filter(f => f.field('Age').greaterThan('20'))
  .sortBy(s => s.by('Name').asc())
  .page(1, 10)
  .toQueryString();

// Result: filters[0][fields]=Name&filters[0][op]=Contains&filters[0][val]=Franco&...
```

## Usage

### Fluent Builder API

The recommended way to build search parameters:

```typescript
import { search } from 'pafiso';

// Complete example
const searchParams = search()
  .filter(f => f.field('Status').equals('active'))
  .filter(f => f.fields('Name', 'Description').contains('search term'))
  .sortBy(s => s.by('CreatedAt').desc())
  .sortBy(s => s.by('Name').asc())
  .page(2, 25)
  .build();

// Get as different formats
const dict = searchParams.toDictionary();           // Record<string, string>
const urlParams = searchParams.toURLSearchParams(); // URLSearchParams
const queryString = searchParams.toQueryString();   // string
```

### Filter Operators

```typescript
import { filter, FilterOperator } from 'pafiso';

// Using builder methods
filter().field('Name').equals('John').build();
filter().field('Age').greaterThan('18').build();
filter().field('Name').contains('test').caseSensitive().build();
filter().field('DeletedAt').isNull().build();

// Available operators
FilterOperator.Equals
FilterOperator.NotEquals
FilterOperator.GreaterThan
FilterOperator.LessThan
FilterOperator.GreaterThanOrEquals
FilterOperator.LessThanOrEquals
FilterOperator.Contains
FilterOperator.NotContains
FilterOperator.Null
FilterOperator.NotNull
```

### Multiple Fields (OR Condition)

When filtering on multiple fields, the filter creates an OR condition:

```typescript
// Matches items where Name OR Description OR Tags contains "search"
filter().fields('Name', 'Description', 'Tags').contains('search').build();
```

### Sorting

```typescript
import { sorting, Sorting, SortOrder } from 'pafiso';

// Using builder
sorting().by('Name').asc().build();
sorting().by('CreatedAt').desc().build();

// Using static helpers
Sorting.asc('Name');
Sorting.desc('CreatedAt');

// Using constructor
new Sorting('Name', SortOrder.Ascending);
```

### Paging

```typescript
import { paging, Paging } from 'pafiso';

// Using builder
paging().page(2, 25).build();  // Page 2, 25 items per page
paging().skip(50).take(25).build();

// Using static helpers
Paging.fromPage(2, 25);    // Page 2, 25 items per page
Paging.fromSkipTake(50, 25);
```

### Direct Class Usage

You can also create instances directly:

```typescript
import { Filter, Sorting, Paging, SearchParameters, FilterOperator, SortOrder } from 'pafiso';

const filter = new Filter('Name', FilterOperator.Contains, 'Franco');
const sorting = new Sorting('CreatedAt', SortOrder.Descending);
const paging = Paging.fromPage(1, 10);

const searchParams = new SearchParameters([filter], [sorting], paging);
```

### Parsing from Query String

```typescript
import { SearchParameters } from 'pafiso';

// From query string
const params = SearchParameters.fromQueryString(
  'filters[0][fields]=Name&filters[0][op]=Contains&filters[0][val]=test&skip=0&take=10'
);

// From URLSearchParams
const urlParams = new URLSearchParams(window.location.search);
const searchParams = SearchParameters.fromURLSearchParams(urlParams);

// From dictionary
const dict = { 'filters[0][fields]': 'Name', 'filters[0][op]': 'Equals', 'filters[0][val]': 'test' };
const parsed = SearchParameters.fromDictionary(dict);
```

## Serialization Format

The library serializes to a format compatible with the .NET Pafiso library:

```
filters[0][fields]=Name
filters[0][op]=Contains
filters[0][val]=Franco
filters[1][fields]=Age
filters[1][op]=GreaterThan
filters[1][val]=20
filters[1][case]=true
sortings[0][prop]=Name
sortings[0][ord]=Ascending
sortings[1][prop]=Age
sortings[1][ord]=Descending
skip=10
take=10
```

## API Reference

### SearchParametersBuilder

| Method | Description |
|--------|-------------|
| `filter(f)` | Add a filter (accepts Filter, FilterBuilder, or callback) |
| `filters(...f)` | Add multiple filters |
| `sortBy(s)` | Add a sorting (accepts Sorting, SortingBuilder, or callback) |
| `sortings(...s)` | Add multiple sortings |
| `paginate(p)` | Set paging (accepts Paging, PagingBuilder, or callback) |
| `page(num, size)` | Set paging by page number (1-based) and size |
| `skipTake(skip, take)` | Set paging by skip and take |
| `build()` | Build SearchParameters instance |
| `toDictionary()` | Build and return as dictionary |
| `toURLSearchParams()` | Build and return as URLSearchParams |
| `toQueryString()` | Build and return as query string |

### FilterBuilder

| Method | Description |
|--------|-------------|
| `field(name)` | Set single field to filter |
| `fields(...names)` | Set multiple fields (OR condition) |
| `op(operator)` | Set filter operator |
| `value(val)` | Set filter value |
| `caseSensitive(enabled?)` | Enable case-sensitive matching |
| `equals(val)` | Shorthand for `op(Equals).value(val)` |
| `notEquals(val)` | Shorthand for `op(NotEquals).value(val)` |
| `greaterThan(val)` | Shorthand for `op(GreaterThan).value(val)` |
| `lessThan(val)` | Shorthand for `op(LessThan).value(val)` |
| `greaterThanOrEquals(val)` | Shorthand for `op(GreaterThanOrEquals).value(val)` |
| `lessThanOrEquals(val)` | Shorthand for `op(LessThanOrEquals).value(val)` |
| `contains(val)` | Shorthand for `op(Contains).value(val)` |
| `notContains(val)` | Shorthand for `op(NotContains).value(val)` |
| `isNull()` | Shorthand for `op(Null)` |
| `isNotNull()` | Shorthand for `op(NotNull)` |
| `build()` | Build Filter instance |

### SortingBuilder

| Method | Description |
|--------|-------------|
| `by(property)` | Set property to sort by |
| `asc()` | Set ascending order |
| `desc()` | Set descending order |
| `build()` | Build Sorting instance |

### PagingBuilder

| Method | Description |
|--------|-------------|
| `skip(count)` | Set records to skip |
| `take(count)` | Set records to take |
| `page(num, size)` | Set by page number (1-based) and size |
| `build()` | Build Paging instance |

## License

MIT
