# ASP.NET Core Integration

The `Pafiso.AspNetCore` package provides seamless integration with ASP.NET Core, including automatic query string parsing, dependency injection support, and configuration synchronization with MVC JSON settings.

## Table of Contents

- [Installation](#installation)
- [Dependency Injection Setup](#dependency-injection-setup)
- [Query String Parsing](#query-string-parsing)
- [Controller Integration](#controller-integration)
- [Auto-Configuration](#auto-configuration)
- [Complete Examples](#complete-examples)

## Installation

Install the ASP.NET Core integration package:

```bash
dotnet add package Pafiso.AspNetCore
```

This package includes:
- `QueryCollectionExtensions` - Extension methods for `IQueryCollection`
- `ServiceCollectionExtensions` - Dependency injection configuration
- Automatic MVC JSON settings integration

## Dependency Injection Setup

### Basic Registration

Register Pafiso in your `Program.cs`:

```csharp
using Pafiso.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Register Pafiso
builder.Services.AddPafiso();

var app = builder.Build();
```

### With Custom Configuration

```csharp
using Pafiso.AspNetCore;
using System.Text.Json;

builder.Services.AddPafiso(settings => {
    settings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    settings.UseJsonPropertyNameAttributes = true;
    settings.StringComparison = StringComparison.OrdinalIgnoreCase;
    settings.UseEfCoreLikeForCaseInsensitive = true;
});
```

### With Pre-Configured Instance

```csharp
var pafisoSettings = new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    UseJsonPropertyNameAttributes = true
};

builder.Services.AddPafiso(pafisoSettings);
```

### Auto-Configuration from MVC

When you call `AddPafiso()` without configuration, it automatically adopts the naming policy from your MVC JSON options:

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Automatically uses CamelCase from above
builder.Services.AddPafiso();
```

## Query String Parsing

The `ToSearchParameters()` extension method converts `IQueryCollection` to `SearchParameters`:

### Basic Usage

```csharp
[HttpGet]
public async Task<IActionResult> GetProducts() {
    // Parse query string directly
    var searchParams = Request.Query.ToSearchParameters();
    
    var pagedQueryable = _dbContext.Products.WithSearchParameters(searchParams);
    var result = await pagedQueryable.ToPagedListAsync();
    
    return Ok(new {
        TotalCount = result.TotalEntries,
        Items = result.Entries
    });
}
```

### With Settings from DI

```csharp
[HttpGet]
public async Task<IActionResult> GetProducts([FromServices] PafisoSettings settings) {
    // Get settings from dependency injection
    var searchParams = Request.Query.ToSearchParameters(settings);
    
    var pagedQueryable = _dbContext.Products.WithSearchParameters(searchParams, settings);
    var result = await pagedQueryable.ToPagedListAsync();
    
    return Ok(new {
        TotalCount = result.TotalEntries,
        Items = result.Entries
    });
}
```

Alternative approach using constructor injection:

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
        
        var pagedQueryable = _dbContext.Products.WithSearchParameters(searchParams, _settings);
        var result = await pagedQueryable.ToPagedListAsync();
        
        return Ok(new {
            TotalCount = result.TotalEntries,
            Items = result.Entries
        });
    }
}
```

### With Explicit Settings

```csharp
[HttpGet]
public async Task<IActionResult> GetProducts() {
    var settings = new PafisoSettings {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    var searchParams = Request.Query.ToSearchParameters(settings);
    
    var pagedQueryable = _dbContext.Products.WithSearchParameters(searchParams, settings);
    var result = await pagedQueryable.ToPagedListAsync();
    
    return Ok(new {
        TotalCount = result.TotalEntries,
        Items = result.Entries
    });
}
```

## Controller Integration

### Basic Controller

```csharp
using Pafiso.AspNetCore;
using Pafiso.EntityFrameworkCore.Enumerables;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase {
    private readonly ApplicationDbContext _dbContext;
    
    public ProductsController(ApplicationDbContext dbContext) {
        _dbContext = dbContext;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetProducts() {
        var searchParams = Request.Query.ToSearchParameters();
        var pagedQueryable = _dbContext.Products.WithSearchParameters(searchParams);
        var result = await pagedQueryable.ToPagedListAsync();
        
        return Ok(new {
            TotalCount = result.TotalEntries,
            Items = result.Entries
        });
    }
}
```

### With Dependency-Injected Settings

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
        var pagedQueryable = _dbContext.Products.WithSearchParameters(searchParams, _settings);
        var result = await pagedQueryable.ToPagedListAsync();
        
        return Ok(new {
            TotalCount = result.TotalEntries,
            Items = result.Entries
        });
    }
}
```

### With Field Restrictions

```csharp
[HttpGet]
public async Task<IActionResult> GetProducts() {
    var searchParams = Request.Query.ToSearchParameters();
    
    var pagedQueryable = _dbContext.Products.WithSearchParameters(
        searchParams,
        restrictions => restrictions
            .AllowFiltering<Product>(x => x.Name, x => x.Price, x => x.Category)
            .AllowSorting<Product>(x => x.Name, x => x.Price, x => x.CreatedDate)
            .BlockFiltering<Product>(x => x.InternalCost, x => x.SupplierId)
    );
    
    var result = await pagedQueryable.ToPagedListAsync();
    
    return Ok(new {
        TotalCount = result.TotalEntries,
        Items = result.Entries
    });
}
```

## Auto-Configuration

### How It Works

When you call `AddPafiso()` without parameters, it:

1. Checks for `IOptions<JsonOptions>` in DI
2. Extracts `PropertyNamingPolicy` from MVC's JSON serializer options
3. Creates `PafisoSettings` with that naming policy
4. Registers the settings as a singleton
5. Sets `PafisoSettings.Default` globally

```csharp
// In Program.cs
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Automatically detects CamelCase
builder.Services.AddPafiso();

// Now both serialization and Pafiso use camelCase
// Your API is consistent!
```

### Override Auto-Configuration

You can override auto-detected values:

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddPafiso(settings => {
    // Override: use snake_case for Pafiso instead
    settings.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    settings.UseEfCoreLikeForCaseInsensitive = true;
});
```

## Complete Examples

### Example 1: Full REST API

```csharp
// Program.cs
using Pafiso.AspNetCore;
using Pafiso.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure MVC
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Register DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Pafiso (auto-detects CamelCase from MVC)
builder.Services.AddPafiso();

// Register EF Core support
EfCoreExpressionBuilder.Register();

var app = builder.Build();

app.MapControllers();
app.Run();
```

```csharp
// ProductsController.cs
using Pafiso.AspNetCore;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase {
    private readonly ApplicationDbContext _dbContext;
    
    public ProductsController(ApplicationDbContext dbContext) {
        _dbContext = dbContext;
    }
    
    /// <summary>
    /// Search products with filtering, sorting, and paging.
    /// </summary>
    /// <remarks>
    /// Example: GET /api/products?skip=0&take=20&filters[0][fields]=name&filters[0][op]=contains&filters[0][val]=laptop&sortings[0][prop]=price&sortings[0][ord]=desc
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetProducts() {
        var searchParams = Request.Query.ToSearchParameters();
        
        var pagedQueryable = _dbContext.Products.WithSearchParameters(
            searchParams,
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
        );
        
        var result = await pagedQueryable.ToPagedListAsync();
        
        return Ok(new ProductListResponse {
            TotalCount = result.TotalEntries,
            Items = result.Entries
        });
    }
    
    /// <summary>
    /// Get a single product by ID.
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult GetProduct(int id) {
        var product = _dbContext.Products.Find(id);
        return product == null ? NotFound() : Ok(product);
    }
}

public class ProductListResponse {
    public int TotalCount { get; set; }
    public List<Product> Items { get; set; }
}
```

### Example 2: Multiple Endpoints with Different Settings

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase {
    private readonly ApplicationDbContext _dbContext;
    private readonly PafisoSettings _publicApiSettings;
    private readonly PafisoSettings _adminApiSettings;
    
    public ProductsController(ApplicationDbContext dbContext) {
        _dbContext = dbContext;
        
        // Public API: limited fields, camelCase
        _publicApiSettings = new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        // Admin API: all fields, no restrictions
        _adminApiSettings = new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
    
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicProducts() {
        var searchParams = Request.Query.ToSearchParameters(_publicApiSettings);
        
        var pagedQueryable = _dbContext.Products.WithSearchParameters(
            searchParams,
            restrictions => restrictions
                .AllowFiltering<Product>(x => x.Name, x => x.Category, x => x.Price)
                .AllowSorting<Product>(x => x.Name, x => x.Price),
            _publicApiSettings
        );
        
        var result = await pagedQueryable.ToPagedListAsync();
        
        return Ok(new {
            TotalCount = result.TotalEntries,
            Items = result.Entries.Select(p => new {
                p.Id,
                p.Name,
                p.Price,
                p.Category
            }).ToList()
        });
    }
    
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminProducts() {
        var searchParams = Request.Query.ToSearchParameters(_adminApiSettings);
        
        // No restrictions for admin
        var pagedQueryable = _dbContext.Products.WithSearchParameters(searchParams, _adminApiSettings);
        var result = await pagedQueryable.ToPagedListAsync();
        
        return Ok(new {
            TotalCount = result.TotalEntries,
            Items = result.Entries  // All fields
        });
    }
}
```

### Example 3: Reusable Base Controller

```csharp
public abstract class SearchableController<T> : ControllerBase where T : class {
    protected readonly ApplicationDbContext DbContext;
    protected readonly PafisoSettings Settings;
    
    protected SearchableController(ApplicationDbContext dbContext, PafisoSettings settings) {
        DbContext = dbContext;
        Settings = settings;
    }
    
    protected async Task<IActionResult> Search(
        IQueryable<T> query,
        Action<FieldRestrictions>? configureRestrictions = null) {
        
        var searchParams = Request.Query.ToSearchParameters(Settings);
        
        var pagedQueryable = configureRestrictions != null
            ? query.WithSearchParameters(searchParams, configureRestrictions, Settings)
            : query.WithSearchParameters(searchParams, Settings);
        
        var result = await pagedQueryable.ToPagedListAsync();
        
        return Ok(new {
            TotalCount = result.TotalEntries,
            Items = result.Entries
        });
    }
}

[ApiController]
[Route("api/[controller]")]
public class ProductsController : SearchableController<Product> {
    public ProductsController(ApplicationDbContext dbContext, PafisoSettings settings) 
        : base(dbContext, settings) { }
    
    [HttpGet]
    public async Task<IActionResult> GetProducts() {
        return await Search(
            DbContext.Products,
            restrictions => restrictions
                .AllowFiltering<Product>(x => x.Name, x => x.Price, x => x.Category)
                .AllowSorting<Product>(x => x.Name, x => x.Price)
        );
    }
}

[ApiController]
[Route("api/[controller]")]
public class OrdersController : SearchableController<Order> {
    public OrdersController(ApplicationDbContext dbContext, PafisoSettings settings) 
        : base(dbContext, settings) { }
    
    [HttpGet]
    public async Task<IActionResult> GetOrders() {
        return await Search(
            DbContext.Orders,
            restrictions => restrictions
                .AllowFiltering<Order>(x => x.OrderDate, x => x.Status, x => x.CustomerId)
                .AllowSorting<Order>(x => x.OrderDate, x => x.TotalAmount)
        );
    }
}
```

### Example 4: Integration with Swagger/OpenAPI

```csharp
// Program.cs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.SwaggerDoc("v1", new OpenApiInfo {
        Title = "Products API",
        Version = "v1",
        Description = "API with Pafiso-powered search, filter, and pagination"
    });
    
    // Add XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

// Controller with documentation
/// <summary>
/// Products API with advanced search capabilities.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase {
    /// <summary>
    /// Search and filter products.
    /// </summary>
    /// <param name="skip">Number of items to skip (default: 0)</param>
    /// <param name="take">Number of items to take (default: 20, max: 100)</param>
    /// <remarks>
    /// Supports filtering and sorting via query parameters.
    /// 
    /// **Filter Format:**
    /// - `filters[0][fields]` - Field name(s) to filter on
    /// - `filters[0][op]` - Operator (eq, neq, gt, lt, gte, lte, contains, ncontains, null, notnull)
    /// - `filters[0][val]` - Value to filter by
    /// 
    /// **Sorting Format:**
    /// - `sortings[0][prop]` - Property name to sort by
    /// - `sortings[0][ord]` - Sort order (asc or desc)
    /// 
    /// **Example:**
    /// 
    ///     GET /api/products?skip=0&take=20&filters[0][fields]=name&filters[0][op]=contains&filters[0][val]=laptop&sortings[0][prop]=price&sortings[0][ord]=desc
    /// </remarks>
    /// <response code="200">Returns the filtered and paginated list of products</response>
    [HttpGet]
    [ProducesResponseType(typeof(ProductListResponse), StatusCodes.Status200OK)]
    public IActionResult GetProducts([FromQuery] int skip = 0, [FromQuery] int take = 20) {
        // Implementation...
    }
}
```

## Best Practices

1. **Call AddPafiso() once** - Register Pafiso in Program.cs, not in multiple places
2. **Use dependency injection** - Inject `PafisoSettings` instead of creating new instances
3. **Match MVC settings** - Keep Pafiso and MVC JSON serialization consistent
4. **Apply restrictions** - Always use field restrictions in public APIs
5. **Document query format** - Provide clear API documentation for filter/sort syntax
6. **Validate page sizes** - Limit maximum page size to prevent abuse
7. **Use HttpContext.RequestServices** - For automatic settings resolution

## Troubleshooting

### Settings Not Auto-Detected

If auto-detection doesn't work:

```csharp
// Ensure MVC is configured BEFORE Pafiso
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddPafiso();  // This must come after AddControllers
```

### Field Names Don't Match

If field names don't resolve correctly:

```csharp
// Debug by checking the settings
var settings = HttpContext.RequestServices.GetService<PafisoSettings>();
Console.WriteLine($"Naming Policy: {settings?.PropertyNamingPolicy}");

// Or explicitly set the naming policy
var searchParams = Request.Query.ToSearchParameters(new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
});
```

## Next Steps

- Learn about [Entity Framework Core Integration](efcore-integration.md) for database optimization
- Implement [Field Restrictions](field-restrictions.md) for API security
- Review [Configuration & Settings](configuration.md) for advanced customization
- See [Advanced Scenarios](advanced-scenarios.md) for complex use cases
