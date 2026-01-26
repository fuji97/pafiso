# Configuration & Settings

Pafiso provides flexible configuration options through the `PafisoSettings` class, allowing you to customize field name mapping, case sensitivity, and Entity Framework Core integration.

## Table of Contents

- [Overview](#overview)
- [PafisoSettings](#pafisosettings)
- [Property Naming Policy](#property-naming-policy)
- [JsonPropertyName Attributes](#jsonpropertyname-attributes)
- [Case Sensitivity](#case-sensitivity)
- [EF Core Like Expressions](#ef-core-like-expressions)
- [Field Name Resolvers](#field-name-resolvers)
- [Global vs Local Settings](#global-vs-local-settings)
- [Examples](#examples)

## Overview

`PafisoSettings` controls how Pafiso interprets field names from query strings and how it performs string comparisons. This is crucial when your API uses different naming conventions than your C# properties (e.g., camelCase in API vs PascalCase in C#).

## PafisoSettings

The `PafisoSettings` class has four main configuration properties:

```csharp
public class PafisoSettings {
    // Field name mapping
    public JsonNamingPolicy? PropertyNamingPolicy { get; set; } = null;
    public bool UseJsonPropertyNameAttributes { get; set; } = true;
    
    // String comparison
    public StringComparison StringComparison { get; set; } = StringComparison.OrdinalIgnoreCase;
    
    // EF Core optimization
    public bool UseEfCoreLikeForCaseInsensitive { get; set; } = true;
    
    // Global default instance
    public static PafisoSettings Default { get; set; } = new();
}
```

### Default Settings

Out of the box, Pafiso uses these defaults:

- No naming policy (field names must match property names exactly, case-insensitive)
- `[JsonPropertyName]` attributes are respected
- Case-insensitive string comparisons
- EF Core LIKE expressions enabled (when EntityFrameworkCore package is installed)

## Property Naming Policy

The `PropertyNamingPolicy` property controls how field names from query strings are mapped to C# property names. Pafiso uses `System.Text.Json.JsonNamingPolicy`:

### Available Policies

```csharp
using System.Text.Json;

// CamelCase: userName -> UserName
var settings = new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

// SnakeCaseLower: user_name -> UserName
var settings = new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
};

// SnakeCaseUpper: USER_NAME -> UserName
var settings = new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper
};

// KebabCaseLower: user-name -> UserName
var settings = new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower
};

// KebabCaseUpper: USER-NAME -> UserName
var settings = new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.KebabCaseUpper
};
```

### How It Works

When a naming policy is set, Pafiso transforms property names and compares them with incoming field names:

```csharp
public class Product {
    public string ProductName { get; set; }  // Property in C#
    public decimal UnitPrice { get; set; }
}

var settings = new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

// Query string: filters[0][fields]=productName&filters[0][op]=contains&filters[0][val]=laptop
// "productName" is resolved to "ProductName" property

var searchParams = SearchParameters.FromDictionary(queryStringDict);
var result = await products
    .WithSearchParameters(searchParams, settings)
    .ToPagedListAsync();
```

### Nested Properties

Naming policy is applied to each level of nested properties:

```csharp
public class Product {
    public Address ShippingAddress { get; set; }
}

public class Address {
    public string StreetName { get; set; }
}

// With CamelCase policy:
// "shippingAddress.streetName" -> "ShippingAddress.StreetName"
var filter = new Filter("shippingAddress.streetName", FilterOperator.Contains, "Main");
```

## JsonPropertyName Attributes

When `UseJsonPropertyNameAttributes` is `true` (default), Pafiso respects `[JsonPropertyName]` attributes:

```csharp
using System.Text.Json.Serialization;

public class Product {
    [JsonPropertyName("product_name")]
    public string Name { get; set; }
    
    [JsonPropertyName("unit_price")]
    public decimal Price { get; set; }
    
    public string Category { get; set; }  // No attribute
}

var settings = new PafisoSettings {
    UseJsonPropertyNameAttributes = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

// These all work:
// "product_name" -> Name (via JsonPropertyName attribute)
// "name" -> Name (via CamelCase policy)
// "category" -> Category (via CamelCase policy)
```

### Attribute Priority

Field name resolution follows this priority:

1. **JsonPropertyName attribute** - If `UseJsonPropertyNameAttributes` is true
2. **Direct property match** - Case-insensitive exact match
3. **Naming policy transformation** - If `PropertyNamingPolicy` is set
4. **Original field name** - As fallback

### Disabling Attributes

```csharp
var settings = new PafisoSettings {
    UseJsonPropertyNameAttributes = false  // Ignore [JsonPropertyName] attributes
};

// Now only direct matches and naming policy apply
```

## Case Sensitivity

The `StringComparison` property controls case sensitivity for string operations (Contains, Equals, etc.) in non-EF Core scenarios:

### Available Options

```csharp
// Case-insensitive (default, most common)
var settings = new PafisoSettings {
    StringComparison = StringComparison.OrdinalIgnoreCase
};

// Case-sensitive
var settings = new PafisoSettings {
    StringComparison = StringComparison.Ordinal
};

// Culture-aware comparisons
var settings = new PafisoSettings {
    StringComparison = StringComparison.CurrentCultureIgnoreCase
};
```

### When It Applies

`StringComparison` is used for:

- In-memory collections (`IEnumerable<T>`)
- `IQueryable<T>` when EF Core LIKE is disabled
- Manual string operations in filters

```csharp
var filter = new Filter("Name", FilterOperator.Contains, "iPhone");

// With OrdinalIgnoreCase (default):
// Matches: "iPhone", "iphone", "IPHONE", "IpHoNe"

// With Ordinal (case-sensitive):
// Matches: "iPhone" only
```

### Per-Filter Case Sensitivity

You can override settings per filter:

```csharp
// Global setting: case-insensitive
PafisoSettings.Default = new PafisoSettings {
    StringComparison = StringComparison.OrdinalIgnoreCase
};

// But this specific filter is case-sensitive
var sensitiveFilter = new Filter("Name", FilterOperator.Equals, "iPhone", caseSensitive: true);
```

## EF Core Like Expressions

When `UseEfCoreLikeForCaseInsensitive` is `true` (default) and you have the `Pafiso.EntityFrameworkCore` package installed, case-insensitive string operations use `EF.Functions.Like()`:

### Setup

```csharp
using Pafiso.EntityFrameworkCore;

// In Program.cs or Startup.cs
EfCoreExpressionBuilder.Register();

// Configure settings
var settings = new PafisoSettings {
    UseEfCoreLikeForCaseInsensitive = true  // Default
};
```

### Benefits

```csharp
// Without EF Core LIKE:
// Generated SQL: WHERE LOWER(Name) LIKE LOWER('%laptop%')
// Slower, can't use indexes efficiently

// With EF Core LIKE:
// Generated SQL: WHERE Name LIKE '%laptop%' COLLATE SQL_Latin1_General_CP1_CI_AS
// Faster, uses database collation, can leverage indexes
```

### When to Disable

Disable EF Core LIKE if:

- You're not using Entity Framework Core
- Your database doesn't support case-insensitive collations
- You need consistent behavior between EF and in-memory queries

```csharp
var settings = new PafisoSettings {
    UseEfCoreLikeForCaseInsensitive = false
};
```

## Field Name Resolvers

Pafiso uses the `IFieldNameResolver` interface to resolve field names. Two implementations are provided:

### DefaultFieldNameResolver

The default resolver that respects settings:

```csharp
var settings = new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    UseJsonPropertyNameAttributes = true
};

var resolver = new DefaultFieldNameResolver(settings);

// Resolves based on policy and attributes
string propertyName = resolver.ResolvePropertyName<Product>("productName");
// Returns: "ProductName"
```

### PassThroughFieldNameResolver

Returns field names unchanged (no transformation):

```csharp
var resolver = PassThroughFieldNameResolver.Instance;

string propertyName = resolver.ResolvePropertyName<Product>("productName");
// Returns: "productName" (unchanged)
```

### Custom Resolvers

Implement `IFieldNameResolver` for custom logic:

```csharp
public class CustomFieldNameResolver : IFieldNameResolver {
    public string ResolvePropertyName<T>(string fieldName) {
        return fieldName switch {
            "id" => "ProductId",
            "name" => "ProductName",
            _ => fieldName
        };
    }
    
    public string ResolvePropertyName(Type targetType, string fieldName) {
        return ResolvePropertyName<object>(fieldName);
    }
}
```

## Global vs Local Settings

### Global Settings

Set default settings for all operations:

```csharp
// In Program.cs or Startup.cs
PafisoSettings.Default = new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    UseJsonPropertyNameAttributes = true,
    StringComparison = StringComparison.OrdinalIgnoreCase,
    UseEfCoreLikeForCaseInsensitive = true
};

// Now all operations use these settings by default
var searchParams = SearchParameters.FromDictionary(dict);
var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(products);
// Uses PafisoSettings.Default
```

### Local Settings

Override settings for specific operations:

```csharp
// Global default
PafisoSettings.Default = new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

// Override for this operation only
var customSettings = new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
};

var result = await products
    .WithSearchParameters(searchParams, customSettings)
    .ToPagedListAsync();
```

### Cloning Settings

```csharp
var baseSettings = PafisoSettings.Default;

// Create a copy with modifications
var modifiedSettings = baseSettings.Clone();
modifiedSettings.StringComparison = StringComparison.Ordinal;

// baseSettings is unchanged
```

## Examples

### Example 1: ASP.NET Core with CamelCase

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Configure Pafiso to match
PafisoSettings.Default = new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    UseJsonPropertyNameAttributes = true
};

// Now API consumers can use camelCase field names
// GET /api/products?filters[0][fields]=productName&filters[0][op]=contains&filters[0][val]=laptop
```

### Example 2: Database with Snake Case

```csharp
public class Product {
    [JsonPropertyName("product_id")]
    public int Id { get; set; }
    
    [JsonPropertyName("product_name")]
    public string Name { get; set; }
    
    [JsonPropertyName("unit_price")]
    public decimal Price { get; set; }
}

var settings = new PafisoSettings {
    UseJsonPropertyNameAttributes = true
};

// Query string uses snake_case
// ?filters[0][fields]=product_name&filters[0][op]=contains&filters[0][val]=laptop
var searchParams = SearchParameters.FromDictionary(queryStringDict);
var result = await products
    .WithSearchParameters(searchParams, settings)
    .ToPagedListAsync();
```

### Example 3: Case-Sensitive Search

```csharp
// Application-wide: case-insensitive
PafisoSettings.Default = new PafisoSettings {
    StringComparison = StringComparison.OrdinalIgnoreCase
};

// But for this specific search, enforce case sensitivity
[HttpGet("products/exact-search")]
public async Task<IActionResult> ExactSearch([FromQuery] string term) {
    var settings = new PafisoSettings {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        StringComparison = StringComparison.Ordinal  // Case-sensitive
    };
    
    var searchParams = new SearchParameters()
        .AddFilters(new Filter("Name", FilterOperator.Equals, term));
    var result = await _dbContext.Products
        .WithSearchParameters(searchParams, settings)
        .ToPagedListAsync();
    
    return Ok(result.Entries);
}
```

### Example 4: Multiple Naming Conventions

```csharp
public class ProductService {
    private readonly DbContext _dbContext;
    private readonly PafisoSettings _internalSettings;
    private readonly PafisoSettings _publicApiSettings;
    
    public ProductService(DbContext dbContext) {
        _dbContext = dbContext;
        
        // Internal API uses PascalCase
        _internalSettings = new PafisoSettings {
            PropertyNamingPolicy = null
        };
        
        // Public API uses camelCase
        _publicApiSettings = new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
    
    public async Task<PagedList<Product>> GetForInternalApi(SearchParameters searchParams) {
        return await _dbContext.Products
            .WithSearchParameters(searchParams, _internalSettings)
            .ToPagedListAsync();
    }
    
    public async Task<PagedList<Product>> GetForPublicApi(SearchParameters searchParams) {
        return await _dbContext.Products
            .WithSearchParameters(searchParams, _publicApiSettings)
            .ToPagedListAsync();
    }
}
```

### Example 5: Custom Field Mapping

```csharp
public class LegacyFieldNameResolver : IFieldNameResolver {
    // Map old API field names to new property names
    private static readonly Dictionary<string, string> _fieldMap = new() {
        ["prod_id"] = "ProductId",
        ["prod_name"] = "Name",
        ["prod_price"] = "Price",
        ["prod_cat"] = "Category"
    };
    
    public string ResolvePropertyName<T>(string fieldName) {
        return _fieldMap.TryGetValue(fieldName, out var propertyName) 
            ? propertyName 
            : fieldName;
    }
    
    public string ResolvePropertyName(Type targetType, string fieldName) {
        return ResolvePropertyName<object>(fieldName);
    }
}

// Usage - note: This requires extending Pafiso or wrapping it
// as IFieldNameResolver is used internally by DefaultFieldNameResolver
```

### Example 6: Environment-Specific Settings

```csharp
public static class PafisoConfiguration {
    public static PafisoSettings CreateSettings(IConfiguration configuration) {
        var env = configuration["Environment"];
        
        return env switch {
            "Development" => new PafisoSettings {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                StringComparison = StringComparison.OrdinalIgnoreCase,
                UseEfCoreLikeForCaseInsensitive = false  // Easier debugging
            },
            "Production" => new PafisoSettings {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                StringComparison = StringComparison.OrdinalIgnoreCase,
                UseEfCoreLikeForCaseInsensitive = true  // Performance optimization
            },
            _ => new PafisoSettings()
        };
    }
}

// In Program.cs
PafisoSettings.Default = PafisoConfiguration.CreateSettings(builder.Configuration);
```

### Example 7: Testing with Different Settings

```csharp
[TestClass]
public class ProductSearchTests {
[TestMethod]
public async Task TestCamelCaseFieldNames() {
    var settings = new PafisoSettings {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    var searchParams = new SearchParameters()
        .AddFilters(new Filter("productName", FilterOperator.Contains, "laptop"));
    var results = await GetTestProducts().AsQueryable()
        .WithSearchParameters(searchParams, settings)
        .ToPagedListAsync();
    
    Assert.IsTrue(results.Entries.All(p => p.ProductName.Contains("laptop", StringComparison.OrdinalIgnoreCase)));
}

[TestMethod]
public async Task TestSnakeCaseFieldNames() {
    var settings = new PafisoSettings {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    
    var searchParams = new SearchParameters()
        .AddFilters(new Filter("product_name", FilterOperator.Contains, "laptop"));
    var results = await GetTestProducts().AsQueryable()
        .WithSearchParameters(searchParams, settings)
        .ToPagedListAsync();
    
    Assert.IsTrue(results.Entries.All(p => p.ProductName.Contains("laptop", StringComparison.OrdinalIgnoreCase)));
}
```

## Best Practices

1. **Set global defaults early** - Configure `PafisoSettings.Default` in your application startup
2. **Match your JSON serialization** - Use the same naming policy as your JSON serializer
3. **Document your convention** - Make it clear to API consumers what naming convention you use
4. **Use attributes for exceptions** - Use `[JsonPropertyName]` for properties that don't follow your naming pattern
5. **Test field resolution** - Write tests to ensure field names resolve correctly
6. **Consider performance** - Enable EF Core LIKE for better database performance
7. **Be consistent** - Use the same settings across your entire API

## Settings Precedence

When multiple settings sources exist:

1. **Local settings passed to method** - Highest priority
2. **Global PafisoSettings.Default** - Fallback
3. **Built-in defaults** - Last resort

```csharp
// 1. Method-level (highest priority)
var localSettings = new PafisoSettings { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
searchParams.ApplyToIQueryable(query, localSettings);

// 2. Global default
PafisoSettings.Default = new PafisoSettings { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
searchParams.ApplyToIQueryable(query);  // Uses Default

// 3. Built-in defaults (if Default not set)
// No naming policy, case-insensitive, attributes enabled
```

## Next Steps

- Learn about [ASP.NET Core Integration](aspnetcore-integration.md) for automatic settings configuration
- Use [Field Restrictions](field-restrictions.md) to secure your API
- Understand [Entity Framework Core Integration](efcore-integration.md) for optimal performance
- Explore [Serialization](serialization.md) to see how settings affect query strings
