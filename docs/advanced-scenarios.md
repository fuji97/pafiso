# Advanced Scenarios

This guide covers advanced use cases and patterns for using Pafiso in complex applications.

## Table of Contents

- [Multi-Tenant Filtering](#multi-tenant-filtering)
- [Nested Property Filtering](#nested-property-filtering)
- [Custom Search Implementations](#custom-search-implementations)
- [Caching Strategies](#caching-strategies)
- [Performance Optimization](#performance-optimization)
- [Testing Patterns](#testing-patterns)
- [Migration Patterns](#migration-patterns)

## Multi-Tenant Filtering

### Automatic Tenant Filtering

```csharp
public class TenantSearchService<T> where T : class, ITenantEntity {
    private readonly DbContext _dbContext;
    private readonly ITenantProvider _tenantProvider;
    
    public async Task<PagedList<T>> SearchAsync(SearchParameters searchParams) {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        
        // Add tenant filter automatically
        var tenantFilter = Filter.FromExpression<T>(x => x.TenantId == tenantId);
        searchParams.AddFilters(tenantFilter);
        
        var query = _dbContext.Set<T>().AsQueryable();
        return await query
            .WithSearchParameters(searchParams)
            .ToPagedListAsync();
    }
}

public interface ITenantEntity {
    int TenantId { get; set; }
}

// Usage
public class ProductsController : ControllerBase {
    private readonly TenantSearchService<Product> _searchService;
    private readonly PafisoSettings _settings;
    
    public ProductsController(TenantSearchService<Product> searchService, PafisoSettings settings) {
        _searchService = searchService;
        _settings = settings;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetProducts() {
        var searchParams = Request.Query.ToSearchParameters(_settings);
        var result = await _searchService.SearchAsync(searchParams);
        
        return Ok(new {
            TotalCount = result.TotalEntries,
            Items = result.Entries
        });
    }
}
```

### Tenant-Specific Restrictions

```csharp
public class TenantRestrictionProvider {
    private readonly ITenantProvider _tenantProvider;
    
    public FieldRestrictions GetRestrictions<T>() {
        var tenant = _tenantProvider.GetCurrentTenant();
        
        return tenant.TenantType switch {
            TenantType.Premium => GetPremiumRestrictions<T>(),
            TenantType.Standard => GetStandardRestrictions<T>(),
            TenantType.Trial => GetTrialRestrictions<T>(),
            _ => new FieldRestrictions()
        };
    }
    
    private FieldRestrictions GetPremiumRestrictions<T>() {
        // Premium tenants can access more fields
        return new FieldRestrictions();  // No restrictions
    }
    
    private FieldRestrictions GetStandardRestrictions<T>() {
        if (typeof(T) == typeof(Product)) {
            return new FieldRestrictions()
                .AllowFiltering<Product>(x => x.Name, x => x.Category, x => x.Price)
                .AllowSorting<Product>(x => x.Name, x => x.Price);
        }
        return new FieldRestrictions();
    }
    
    private FieldRestrictions GetTrialRestrictions<T>() {
        if (typeof(T) == typeof(Product)) {
            return new FieldRestrictions()
                .AllowFiltering<Product>(x => x.Name, x => x.Category)
                .AllowSorting<Product>(x => x.Name);
        }
        return new FieldRestrictions();
    }
}
```

## Nested Property Filtering

### Filtering on Navigation Properties

```csharp
public class Product {
    public int Id { get; set; }
    public string Name { get; set; }
    public Category Category { get; set; }
    public Supplier Supplier { get; set; }
}

public class Category {
    public int Id { get; set; }
    public string Name { get; set; }
}

// Filter on nested property
var settings = new PafisoSettings {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

// Query string: ?filters[0][fields]=category.name&filters[0][op]=eq&filters[0][val]=Electronics
var searchParams = Request.Query.ToSearchParameters();

// Field name "category.name" is resolved to "Category.Name"
var result = await _dbContext.Products
    .Include(p => p.Category)
    .WithSearchParameters(searchParams, settings)
    .ToPagedListAsync();
```

### Complex Nested Filtering

```csharp
public class Order {
    public int Id { get; set; }
    public Customer Customer { get; set; }
    public List<OrderItem> Items { get; set; }
}

public class Customer {
    public int Id { get; set; }
    public string Name { get; set; }
    public Address Address { get; set; }
}

public class Address {
    public string City { get; set; }
    public string Country { get; set; }
}

// Filter on deeply nested property: customer.address.city
[HttpGet]
public async Task<IActionResult> GetOrders([FromServices] PafisoSettings settings) {
    var searchParams = Request.Query.ToSearchParameters(settings);
    
    var result = await _dbContext.Orders
        .Include(o => o.Customer)
        .ThenInclude(c => c.Address)
        .WithSearchParameters(searchParams, settings)
        .ToPagedListAsync();
    
    return Ok(new {
        TotalCount = result.TotalEntries,
        Items = result.Entries
    });
}

// Query: ?filters[0][fields]=customer.address.city&filters[0][op]=eq&filters[0][val]=Seattle
```

## Custom Search Implementations

### Saved Searches

```csharp
public class SavedSearch {
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Name { get; set; }
    public string EntityType { get; set; }
    public string SearchParametersJson { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class SavedSearchService {
    private readonly ApplicationDbContext _dbContext;
    
    public async Task<int> SaveSearchAsync(string userId, string name, string entityType, SearchParameters searchParams) {
        var dict = searchParams.ToDictionary();
        var json = JsonSerializer.Serialize(dict);
        
        var savedSearch = new SavedSearch {
            UserId = userId,
            Name = name,
            EntityType = entityType,
            SearchParametersJson = json,
            CreatedDate = DateTime.UtcNow
        };
        
        _dbContext.SavedSearches.Add(savedSearch);
        await _dbContext.SaveChangesAsync();
        
        return savedSearch.Id;
    }
    
    public async Task<SearchParameters?> LoadSearchAsync(int searchId, string userId) {
        var savedSearch = await _dbContext.SavedSearches
            .Where(s => s.Id == searchId && s.UserId == userId)
            .FirstOrDefaultAsync();
        
        if (savedSearch == null) return null;
        
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(savedSearch.SearchParametersJson);
        return SearchParameters.FromDictionary(dict);
    }
    
    public async Task<List<SavedSearch>> GetUserSearchesAsync(string userId, string entityType) {
        return await _dbContext.SavedSearches
            .Where(s => s.UserId == userId && s.EntityType == entityType)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync();
    }
}

// Controller usage
[HttpPost("searches")]
public async Task<IActionResult> SaveSearch([FromBody] SaveSearchRequest request) {
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var searchParams = SearchParameters.FromDictionary(request.SearchParameters);
    
    var searchId = await _savedSearchService.SaveSearchAsync(
        userId,
        request.Name,
        "Product",
        searchParams
    );
    
    return Ok(new { SearchId = searchId });
}

[HttpGet("searches/{id}")]
public async Task<IActionResult> ExecuteSavedSearch(int id, [FromServices] PafisoSettings settings) {
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var searchParams = await _savedSearchService.LoadSearchAsync(id, userId);
    
    if (searchParams == null) return NotFound();
    
    var result = await _dbContext.Products
        .WithSearchParameters(searchParams, settings)
        .ToPagedListAsync();
    
    return Ok(new {
        TotalCount = result.TotalEntries,
        Items = result.Entries
    });
}
```

### Search Analytics

```csharp
public class SearchAnalytics {
    public int Id { get; set; }
    public string UserId { get; set; }
    public string EntityType { get; set; }
    public string SearchParametersJson { get; set; }
    public int ResultCount { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public DateTime SearchDate { get; set; }
}

public class AnalyticsSearchWrapper {
    private readonly ApplicationDbContext _dbContext;
    
    public async Task<PagedList<T>> SearchWithAnalyticsAsync<T>(
        string userId,
        string entityType,
        SearchParameters searchParams,
        IQueryable<T> query) where T : class {
        
        var sw = Stopwatch.StartNew();
        
        var result = await query
            .WithSearchParameters(searchParams)
            .ToPagedListAsync();
        
        sw.Stop();
        
        // Log analytics
        var analytics = new SearchAnalytics {
            UserId = userId,
            EntityType = entityType,
            SearchParametersJson = JsonSerializer.Serialize(searchParams.ToDictionary()),
            ResultCount = result.TotalEntries,
            ExecutionTime = sw.Elapsed,
            SearchDate = DateTime.UtcNow
        };
        
        _dbContext.SearchAnalytics.Add(analytics);
        await _dbContext.SaveChangesAsync();
        
        return result;
    }
```

## Caching Strategies

### Cache Search Results

```csharp
public class CachedSearchService<T> where T : class {
    private readonly IMemoryCache _cache;
    private readonly ApplicationDbContext _dbContext;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
    
    public async Task<PagedList<T>> SearchAsync(SearchParameters searchParams) {
        var cacheKey = GenerateCacheKey(searchParams);
        
        if (_cache.TryGetValue<PagedList<T>>(cacheKey, out var cached)) {
            return cached;
        }
        
        var query = _dbContext.Set<T>().AsQueryable();
        var result = await query
            .WithSearchParameters(searchParams)
            .ToPagedListAsync();
        
        _cache.Set(cacheKey, result, _cacheExpiration);
        
        return result;
    }
```

### Cache Invalidation

```csharp
public class SmartCacheService<T> where T : class {
    private readonly IMemoryCache _cache;
    private readonly IDistributedCache _distributedCache;
    
    public async Task InvalidateCacheForEntity() {
        // Invalidate all cached searches for this entity type
        var cacheKey = $"search_{typeof(T).Name}_*";
        
        // In production, use a more sophisticated cache invalidation strategy
        // such as Redis key patterns or cache tags
    }
    
    public void OnEntityModified(T entity) {
        // Invalidate related caches when entity is modified
        InvalidateCacheForEntity().Wait();
    }
}

// In your repository or DbContext SaveChanges override
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
    var entries = ChangeTracker.Entries()
        .Where(e => e.State == EntityState.Added || 
                    e.State == EntityState.Modified || 
                    e.State == EntityState.Deleted);
    
    var result = await base.SaveChangesAsync(cancellationToken);
    
    // Invalidate caches for modified entities
    foreach (var entry in entries) {
        var entityType = entry.Entity.GetType();
        // Trigger cache invalidation
    }
    
    return result;
}
```

## Performance Optimization

### Projection for Large Objects

```csharp
public class OptimizedSearchService {
    public async Task<PagedResult<ProductDto>> SearchProductsAsync(SearchParameters searchParams) {
        var pagedQueryable = _dbContext.Products
            .WithSearchParameters(searchParams);
        
        // Project to DTO before materialization
        var dtoQuery = pagedQueryable.Select(p => new ProductDto {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            CategoryName = p.Category.Name
            // Only include fields you need
        });
        
        var result = await dtoQuery.ToPagedListAsync();
        
        return new PagedResult<ProductDto> {
            Items = result.Entries,
            TotalCount = result.TotalEntries
        };
    }
}
```

### Batch Operations

```csharp
public class BatchSearchService {
    public async Task<Dictionary<string, PagedQueryable<Product>>> SearchMultipleCategoriesAsync(
        string[] categories,
        int pageSize = 20) {
        
        var results = new Dictionary<string, PagedQueryable<Product>>();
        
        // Create search parameters for each category
        var searchTasks = categories.Select(async category => {
            var searchParams = new SearchParameters(Paging.FromPaging(0, pageSize))
                .AddFilters(Filter.FromExpression<Product>(x => x.Category == category))
                .AddSorting(Sorting.FromExpression<Product>(x => x.Name, SortOrder.Ascending));
            
            var result = await _dbContext.Products
                .WithSearchParameters(searchParams)
                .ToPagedListAsync();
            
            return (category, result);
        });
        
        var searchResults = await Task.WhenAll(searchTasks);
        
        foreach (var (category, result) in searchResults) {
            results[category] = result;
        }
        
        return results;
    }
}
```

## Testing Patterns

### Unit Testing with In-Memory Data

```csharp
[TestClass]
public class SearchParametersTests {
    private List<Product> GetTestData() {
        return new List<Product> {
            new() { Id = 1, Name = "Laptop", Price = 999, Category = "Electronics" },
            new() { Id = 2, Name = "Mouse", Price = 25, Category = "Electronics" },
            new() { Id = 3, Name = "Desk", Price = 300, Category = "Furniture" }
        };
    }
    
[TestMethod]
public async Task TestFilteringAndPaging() {
    var data = GetTestData();
    var query = data.AsQueryable();
    
    var searchParams = new SearchParameters(Paging.FromPaging(0, 2))
        .AddFilters(Filter.FromExpression<Product>(x => x.Category == "Electronics"));
    
    var result = await query
        .WithSearchParameters(searchParams)
        .ToPagedListAsync();
    
    Assert.AreEqual(2, result.TotalEntries);
    Assert.AreEqual(2, result.Entries.Count);
}

[TestMethod]
public async Task TestSorting() {
    var data = GetTestData();
    var query = data.AsQueryable();
    
    var searchParams = new SearchParameters()
        .AddSorting(Sorting.FromExpression<Product>(x => x.Price, SortOrder.Descending));
    
    var result = await query
        .WithSearchParameters(searchParams)
        .ToPagedListAsync();
    
    Assert.AreEqual(999, result.Entries[0].Price);
    Assert.AreEqual(25, result.Entries[2].Price);
}
```

### Integration Testing with Test Database

```csharp
[TestClass]
public class ProductSearchIntegrationTests {
    private ApplicationDbContext _dbContext;
    
    [TestInitialize]
    public void Setup() {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        
        _dbContext = new ApplicationDbContext(options);
        
        // Seed test data
        _dbContext.Products.AddRange(
            new Product { Name = "Laptop", Price = 999, Category = "Electronics" },
            new Product { Name = "Mouse", Price = 25, Category = "Electronics" },
            new Product { Name = "Desk", Price = 300, Category = "Furniture" }
        );
        _dbContext.SaveChanges();
    }
    
[TestMethod]
public async Task TestCompleteSearchFlow() {
    var searchParams = new SearchParameters(Paging.FromPaging(0, 10))
        .AddFilters(Filter.FromExpression<Product>(x => x.Price > 50))
        .AddSorting(Sorting.FromExpression<Product>(x => x.Price, SortOrder.Ascending));
    
    var result = await _dbContext.Products
        .WithSearchParameters(searchParams)
        .ToPagedListAsync();
    
    Assert.AreEqual(2, result.TotalEntries);
    Assert.AreEqual("Desk", result.Entries[0].Name);
    Assert.AreEqual("Laptop", result.Entries[1].Name);
}
```

## Migration Patterns

### From Manual Query Building

```csharp
// Before Pafiso
public List<Product> SearchProducts(ProductFilter filter) {
    var query = _dbContext.Products.AsQueryable();
    
    if (!string.IsNullOrEmpty(filter.Name)) {
        query = query.Where(p => p.Name.Contains(filter.Name));
    }
    
    if (filter.MinPrice.HasValue) {
        query = query.Where(p => p.Price >= filter.MinPrice.Value);
    }
    
    if (!string.IsNullOrEmpty(filter.SortBy)) {
        query = filter.SortBy switch {
            "name" => query.OrderBy(p => p.Name),
            "price" => query.OrderBy(p => p.Price),
            _ => query
        };
    }
    
    return query.Skip(filter.Skip).Take(filter.Take).ToList();
}

// After Pafiso
public async Task<PagedList<Product>> SearchProducts([FromServices] PafisoSettings settings) {
    var searchParams = Request.Query.ToSearchParameters(settings);
    return await _dbContext.Products
        .WithSearchParameters(
            searchParams,
            settings,
            restrictions => restrictions
                .AllowFiltering<Product>(x => x.Name, x => x.Price)
                .AllowSorting<Product>(x => x.Name, x => x.Price)
        )
        .ToPagedListAsync();
}
```

## Next Steps

- Review all [documentation guides](README.md) for comprehensive coverage
- Check out the [GitHub repository](https://github.com/fuji97/pafiso) for source code and examples
- Join discussions for questions and feature requests
