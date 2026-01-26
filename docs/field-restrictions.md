# Field Restrictions

Field restrictions provide server-side security controls to prevent unauthorized filtering or sorting on sensitive fields. This is essential for public APIs where you cannot trust client input.

## Table of Contents

- [Why Field Restrictions?](#why-field-restrictions)
- [Basic Usage](#basic-usage)
- [Allowlist vs Blocklist](#allowlist-vs-blocklist)
- [Expression-Based vs String-Based](#expression-based-vs-string-based)
- [Applying Restrictions](#applying-restrictions)
- [Behavior](#behavior)
- [Examples](#examples)

## Why Field Restrictions?

Without restrictions, API consumers could filter or sort on any field:

```csharp
// User sends: ?filters[0][fields]=InternalCost&filters[0][op]=gt&filters[0][val]=0
// This exposes sensitive internal cost data!

// Or: ?sortings[0][prop]=SupplierSecret&sortings[0][ord]=asc
// This could leak supplier information through sort order!
```

Field restrictions prevent these security issues by explicitly controlling which fields can be filtered or sorted.

## Basic Usage

### Creating Restrictions

```csharp
var restrictions = new FieldRestrictions();

// Allow filtering on specific fields
restrictions.AllowFiltering<Product>(x => x.Name, x => x.Price, x => x.Category);

// Allow sorting on specific fields
restrictions.AllowSorting<Product>(x => x.Name, x => x.Price, x => x.CreatedDate);

// Block specific fields from filtering
restrictions.BlockFiltering<Product>(x => x.InternalCost, x => x.SupplierId);

// Block specific fields from sorting
restrictions.BlockSorting<Product>(x => x.InternalRevenue);
```

### Applying to Queries

```csharp
[HttpGet]
public async Task<IActionResult> GetProducts([FromServices] PafisoSettings settings) {
    var searchParams = Request.Query.ToSearchParameters(settings);
    
    var restrictions = new FieldRestrictions()
        .AllowFiltering<Product>(x => x.Name, x => x.Price, x => x.Category)
        .AllowSorting<Product>(x => x.Name, x => x.Price);
    
    var result = await _dbContext.Products
        .WithSearchParameters(searchParams, settings, restrictions)
        .ToPagedListAsync();
    
    return Ok(new { TotalCount = result.TotalEntries, Items = result.Entries });
}
```

### Inline Configuration

```csharp
var result = await _dbContext.Products
    .WithSearchParameters(
        searchParams,
        restrictions: restrictions => restrictions
            .AllowFiltering<Product>(x => x.Name, x => x.Price, x => x.Category)
            .AllowSorting<Product>(x => x.Name, x => x.Price)
            .BlockFiltering<Product>(x => x.InternalCost)
    )
    .ToPagedListAsync();
```

## Allowlist vs Blocklist

### Allowlist Mode (Recommended)

Only explicitly allowed fields are permitted:

```csharp
var restrictions = new FieldRestrictions()
    .AllowFiltering<Product>(x => x.Name, x => x.Price);  // ONLY these fields

// Name: ✓ Allowed
// Price: ✓ Allowed
// InternalCost: ✗ Blocked (not in allowlist)
// SupplierId: ✗ Blocked (not in allowlist)
```

**Best for:** Public APIs, untrusted clients

### Blocklist Mode

All fields allowed except explicitly blocked ones:

```csharp
var restrictions = new FieldRestrictions()
    .BlockFiltering<Product>(x => x.InternalCost, x => x.SupplierId);

// Name: ✓ Allowed (not blocked)
// Price: ✓ Allowed (not blocked)
// InternalCost: ✗ Blocked (explicitly blocked)
// SupplierId: ✗ Blocked (explicitly blocked)
```

**Best for:** Internal APIs, trusted clients

### Combined Mode

Blocklist takes precedence over allowlist:

```csharp
var restrictions = new FieldRestrictions()
    .AllowFiltering<Product>(x => x.Name, x => x.Price, x => x.InternalCost)
    .BlockFiltering<Product>(x => x.InternalCost);  // Blocks even if allowed

// Name: ✓ Allowed
// Price: ✓ Allowed
// InternalCost: ✗ Blocked (blocklist wins)
```

## Expression-Based vs String-Based

### Expression-Based (Type-Safe)

Recommended for compile-time safety:

```csharp
restrictions.AllowFiltering<Product>(
    x => x.Name,
    x => x.Price,
    x => x.Category
);

// Compile error if field doesn't exist
// restrictions.AllowFiltering<Product>(x => x.NonExistentField);  // Won't compile
```

### String-Based

Useful for dynamic scenarios:

```csharp
restrictions.AllowFiltering("Name", "Price", "Category");

// Or dynamically
var allowedFields = configuration.GetSection("AllowedFilterFields").Get<string[]>();
restrictions.AllowFiltering(allowedFields);
```

## Applying Restrictions

### With SearchParameters

```csharp
var searchParams = new SearchParameters(Paging.FromPaging(0, 20))
    .AddFilters(new Filter("Name", FilterOperator.Contains, "laptop"))
    .AddSorting(new Sorting("Price", SortOrder.Descending));

var result = await products
    .WithSearchParameters(
        searchParams,
        restrictions: restrictions => restrictions
            .AllowFiltering<Product>(x => x.Name)
            .AllowSorting<Product>(x => x.Price)
    )
    .ToPagedListAsync();
```

### With Individual Operations

```csharp
var filter = new Filter("InternalCost", FilterOperator.GreaterThan, "100");
var restrictions = new FieldRestrictions()
    .BlockFiltering<Product>(x => x.InternalCost);

// Filter is applied with restrictions - InternalCost will be ignored
var results = filter.ApplyFilter(products, restrictions).ToList();
```

### With Sorting

```csharp
var sorting = new Sorting("InternalRevenue", SortOrder.Descending);
var restrictions = new FieldRestrictions()
    .BlockSorting<Product>(x => x.InternalRevenue);

// Returns null if field is blocked
var ordered = sorting.ApplyToIQueryable(products, restrictions);
if (ordered == null) {
    // Sorting was blocked, use default
    ordered = products.OrderBy(p => p.Name);
}
```

## Behavior

### Filtering Behavior

When a filter contains blocked fields:

```csharp
// Filter on multiple fields (OR condition)
var filter = new Filter(
    new[] { "Name", "InternalCost" },  // Name is allowed, InternalCost is blocked
    FilterOperator.Contains,
    "laptop"
);

var restrictions = new FieldRestrictions()
    .AllowFiltering<Product>(x => x.Name);

var results = filter.ApplyFilter(products, restrictions);
// Only filters on Name, InternalCost is silently ignored
```

If all fields in a filter are blocked:

```csharp
var filter = new Filter("InternalCost", FilterOperator.GreaterThan, "100");

var restrictions = new FieldRestrictions()
    .AllowFiltering<Product>(x => x.Name);  // InternalCost not allowed

var results = filter.ApplyFilter(products, restrictions);
// Returns original query unchanged (no filtering applied)
```

### Sorting Behavior

When primary sort is blocked:

```csharp
var sortings = new List<Sorting> {
    new Sorting("InternalRevenue", SortOrder.Descending),  // Blocked
    new Sorting("Price", SortOrder.Descending)             // Allowed
};

var restrictions = new FieldRestrictions()
    .AllowSorting<Product>(x => x.Price);

// First allowed sorting becomes primary sort
// InternalRevenue is skipped, Price is used
```

When all sortings are blocked:

```csharp
var sorting = new Sorting("InternalRevenue", SortOrder.Descending);

var restrictions = new FieldRestrictions()
    .AllowSorting<Product>(x => x.Name);

var ordered = sorting.ApplyToIQueryable(products, restrictions);
// Returns null - caller should provide default sort
```

### Silent vs Explicit Failure

Pafiso uses **silent failure** for restrictions:

- Blocked filters are ignored
- Blocked sortings return null
- No exceptions thrown
- No error messages to client

This prevents information disclosure about your data model.

## Examples

### Example 1: Public Product API

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase {
    private readonly ApplicationDbContext _dbContext;
    private readonly PafisoSettings _settings;
    
    public ProductsController(ApplicationDbContext dbContext, PafisoSettings settings) {
        _dbContext = dbContext;
        _settings = settings;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetProducts() {
        var searchParams = Request.Query.ToSearchParameters(_settings);
        
        // Strict allowlist for public API
        var result = await _dbContext.Products
            .WithSearchParameters(
                searchParams,
                _settings,
                restrictions => restrictions
                    .AllowFiltering<Product>(
                        x => x.Name,
                        x => x.Description,
                        x => x.Category,
                        x => x.Price,
                        x => x.InStock
                    )
                    .AllowSorting<Product>(
                        x => x.Name,
                        x => x.Price,
                        x => x.CreatedDate,
                        x => x.Rating
                    )
            )
            .ToPagedListAsync();
        
        return Ok(new { TotalCount = result.TotalEntries, Items = result.Entries });
    }
}
```

### Example 2: Role-Based Restrictions

```csharp
[HttpGet]
public async Task<IActionResult> GetProducts([FromServices] PafisoSettings settings) {
    var searchParams = Request.Query.ToSearchParameters(settings);
    
    var restrictions = new FieldRestrictions();
    
    if (User.IsInRole("Admin")) {
        // Admins can filter/sort on everything
        // No restrictions applied
    } else if (User.IsInRole("Manager")) {
        // Managers can see most fields, but not internal costs
        restrictions
            .AllowFiltering<Product>(x => x.Name, x => x.Price, x => x.Stock, x => x.SupplierId)
            .AllowSorting<Product>(x => x.Name, x => x.Price, x => x.Stock)
            .BlockFiltering<Product>(x => x.InternalCost)
            .BlockSorting<Product>(x => x.InternalCost);
    } else {
        // Regular users have limited access
        restrictions
            .AllowFiltering<Product>(x => x.Name, x => x.Category, x => x.Price)
            .AllowSorting<Product>(x => x.Name, x => x.Price, x => x.CreatedDate);
    }
    
    var result = await _dbContext.Products
        .WithSearchParameters(searchParams, settings, restrictions)
        .ToPagedListAsync();
    
    return Ok(new { TotalCount = result.TotalEntries, Items = result.Entries });
}
```

### Example 3: Configuration-Driven Restrictions

```csharp
// appsettings.json
{
  "ApiRestrictions": {
    "Products": {
      "AllowedFilterFields": ["Name", "Category", "Price"],
      "AllowedSortFields": ["Name", "Price", "CreatedDate"],
      "BlockedFilterFields": ["InternalCost", "SupplierId"],
      "BlockedSortFields": ["InternalRevenue"]
    }
  }
}

// Service
public class RestrictionService {
    private readonly IConfiguration _configuration;
    
    public FieldRestrictions GetProductRestrictions() {
        var config = _configuration.GetSection("ApiRestrictions:Products");
        
        var restrictions = new FieldRestrictions();
        
        var allowedFilterFields = config.GetSection("AllowedFilterFields").Get<string[]>();
        if (allowedFilterFields != null) {
            restrictions.AllowFiltering(allowedFilterFields);
        }
        
        var allowedSortFields = config.GetSection("AllowedSortFields").Get<string[]>();
        if (allowedSortFields != null) {
            restrictions.AllowSorting(allowedSortFields);
        }
        
        var blockedFilterFields = config.GetSection("BlockedFilterFields").Get<string[]>();
        if (blockedFilterFields != null) {
            restrictions.BlockFiltering(blockedFilterFields);
        }
        
        var blockedSortFields = config.GetSection("BlockedSortFields").Get<string[]>();
        if (blockedSortFields != null) {
            restrictions.BlockSorting(blockedSortFields);
        }
        
        return restrictions;
    }
}

// Controller
[HttpGet]
public async Task<IActionResult> GetProducts([FromServices] RestrictionService restrictionService, [FromServices] PafisoSettings settings) {
    var searchParams = Request.Query.ToSearchParameters(settings);
    var restrictions = restrictionService.GetProductRestrictions();
    
    var result = await _dbContext.Products
        .WithSearchParameters(searchParams, settings, restrictions)
        .ToPagedListAsync();
    
    return Ok(new { TotalCount = result.TotalEntries, Items = result.Entries });
}
```

### Example 4: Reusable Restrictions

```csharp
public static class ProductRestrictions {
    public static FieldRestrictions Public => new FieldRestrictions()
        .AllowFiltering<Product>(x => x.Name, x => x.Category, x => x.Price)
        .AllowSorting<Product>(x => x.Name, x => x.Price);
    
    public static FieldRestrictions Internal => new FieldRestrictions()
        .AllowFiltering<Product>(x => x.Name, x => x.Price, x => x.Stock, x => x.SupplierId)
        .AllowSorting<Product>(x => x.Name, x => x.Price, x => x.Stock)
        .BlockFiltering<Product>(x => x.InternalCost);
    
    public static FieldRestrictions Admin => new FieldRestrictions();  // No restrictions
}

// Usage
[HttpGet("public")]
public async Task<IActionResult> GetPublicProducts([FromServices] PafisoSettings settings) {
    var searchParams = Request.Query.ToSearchParameters(settings);
    var result = await _dbContext.Products
        .WithSearchParameters(searchParams, settings, ProductRestrictions.Public)
        .ToPagedListAsync();
    return Ok(new { TotalCount = result.TotalEntries, Items = result.Entries });
}

[HttpGet("internal")]
[Authorize]
public async Task<IActionResult> GetInternalProducts([FromServices] PafisoSettings settings) {
    var searchParams = Request.Query.ToSearchParameters(settings);
    var result = await _dbContext.Products
        .WithSearchParameters(searchParams, settings, ProductRestrictions.Internal)
        .ToPagedListAsync();
    return Ok(new { TotalCount = result.TotalEntries, Items = result.Entries });
}
```

## Best Practices

1. **Always use restrictions in public APIs** - Never trust client input
2. **Prefer allowlist over blocklist** - Explicit is better than implicit
3. **Use expression-based restrictions** - Get compile-time safety
4. **Document allowed fields** - Make it clear to API consumers what they can filter/sort on
5. **Test restrictions** - Verify blocked fields are actually blocked
6. **Use role-based restrictions** - Different restrictions for different user roles
7. **Don't expose sensitive data** - Block fields containing secrets, costs, internal IDs, etc.

## Security Considerations

### Common Sensitive Fields

Always restrict these types of fields:

- Internal costs and pricing
- Supplier/vendor information
- Internal IDs and references
- Audit fields (CreatedBy, ModifiedBy)
- Soft delete flags
- Security-related fields
- Calculated/derived sensitive data

### Information Disclosure

Even sort order can leak information:

```csharp
// Bad: Allowing sort on InternalCost
// User can deduce cost by observing sort order

// Good: Block sensitive sorting
restrictions.BlockSorting<Product>(x => x.InternalCost);
```

### Combined with Authorization

Restrictions complement but don't replace authorization:

```csharp
[HttpGet]
[Authorize]  // Still need authorization!
public IActionResult GetProducts() {
    // Restrictions control what fields can be queried
    // Authorization controls who can access the endpoint
}
```

## Next Steps

- Learn about [Configuration & Settings](configuration.md) for field name mapping
- See [ASP.NET Core Integration](aspnetcore-integration.md) for web API setup
- Review [Advanced Scenarios](advanced-scenarios.md) for complex security patterns
