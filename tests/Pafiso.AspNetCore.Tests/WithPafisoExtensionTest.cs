using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using Pafiso.AspNetCore;
using Shouldly;

namespace Pafiso.AspNetCore.Tests;

public class WithPafisoExtensionTest {
    // Test DTO classes
    public class ProductFilterDto : MappingModel {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class ProductSortDto : MappingModel {
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    // Test entity class
    public class Product {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    private static IQueryCollection CreateQueryCollection(Dictionary<string, string> values) {
        var dict = values.ToDictionary(
            kvp => kvp.Key,
            kvp => new StringValues(kvp.Value)
        );
        return new QueryCollection(dict);
    }

    [Test]
    public void WithPafiso_WithoutConfiguration_ReturnsAllItems() {
        // Arrange
        var products = new List<Product> {
            new() { Id = 1, Name = "Product A", Category = "Cat1", Price = 10 },
            new() { Id = 2, Name = "Product B", Category = "Cat2", Price = 20 },
            new() { Id = 3, Name = "Product C", Category = "Cat1", Price = 30 }
        }.AsQueryable();

        var queryCollection = CreateQueryCollection(new Dictionary<string, string>());

        // Act
        var result = products.WithPafiso(queryCollection);
        var pagedList = result.ToPagedList();

        // Assert
        pagedList.TotalEntries.ShouldBe(3);
        pagedList.Count.ShouldBe(3);
    }

    [Test]
    public void WithPafiso_WithPaging_ReturnsPaged() {
        // Arrange
        var products = new List<Product> {
            new() { Id = 1, Name = "Product A", Category = "Cat1", Price = 10 },
            new() { Id = 2, Name = "Product B", Category = "Cat2", Price = 20 },
            new() { Id = 3, Name = "Product C", Category = "Cat1", Price = 30 },
            new() { Id = 4, Name = "Product D", Category = "Cat2", Price = 40 }
        }.AsQueryable();

        var queryCollection = CreateQueryCollection(new Dictionary<string, string> {
            ["skip"] = "0",
            ["take"] = "2"
        });

        // Act
        var result = products.WithPafiso(queryCollection, configure: opt => {
            opt.WithPaging();
        });
        var pagedList = result.ToPagedList();

        // Assert
        pagedList.TotalEntries.ShouldBe(4);
        pagedList.Count.ShouldBe(2);
        pagedList[0].Id.ShouldBe(1);
        pagedList[1].Id.ShouldBe(2);
    }

    [Test]
    public void WithPafiso_WithFiltering_1to1Mapping_FiltersCorrectly() {
        // Arrange
        var products = new List<Product> {
            new() { Id = 1, Name = "Product A", Category = "Electronics", Price = 10 },
            new() { Id = 2, Name = "Product B", Category = "Books", Price = 20 },
            new() { Id = 3, Name = "Product C", Category = "Electronics", Price = 30 }
        }.AsQueryable();

        var queryCollection = CreateQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"] = "eq",
            ["filters[0][val]"] = "Electronics"
        });

        // Act
        var result = products.WithPafiso(queryCollection, configure: opt => {
            opt.WithFiltering<ProductFilterDto>();
        });
        var pagedList = result.ToPagedList();

        // Assert
        pagedList.TotalEntries.ShouldBe(2);
        pagedList.ShouldAllBe(p => p.Category == "Electronics");
    }

    [Test]
    public void WithPafiso_WithFiltering_CustomMapping_FiltersCorrectly() {
        // Arrange
        var products = new List<Product> {
            new() { Id = 1, Name = "Product A", Category = "Cat1", Price = 10 },
            new() { Id = 2, Name = "Product B", Category = "Cat2", Price = 20 },
            new() { Id = 3, Name = "Product C", Category = "Cat1", Price = 30 }
        }.AsQueryable();

        var queryCollection = CreateQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "ProductId",
            ["filters[0][op]"] = "eq",
            ["filters[0][val]"] = "2"
        });

        // Act
        var result = products.WithPafiso(queryCollection, configure: opt => {
            opt.WithFiltering<ProductFilterDto>()
                .Map(dto => dto.ProductId, entity => entity.Id);
        });
        var pagedList = result.ToPagedList();

        // Assert
        pagedList.Count.ShouldBe(1);
        pagedList[0].Id.ShouldBe(2);
    }

    [Test]
    public void WithPafiso_WithSorting_1to1Mapping_SortsCorrectly() {
        // Arrange
        var products = new List<Product> {
            new() { Id = 1, Name = "Product C", Category = "Cat1", Price = 10 },
            new() { Id = 2, Name = "Product A", Category = "Cat2", Price = 20 },
            new() { Id = 3, Name = "Product B", Category = "Cat1", Price = 30 }
        }.AsQueryable();

        var queryCollection = CreateQueryCollection(new Dictionary<string, string> {
            ["sortings[0][prop]"] = "Category",
            ["sortings[0][ord]"] = "asc"
        });

        // Act
        var result = products.WithPafiso(queryCollection, configure: opt => {
            opt.WithSorting<ProductSortDto>();
        });
        var pagedList = result.ToPagedList();

        // Assert
        pagedList.Count.ShouldBe(3);
        pagedList[0].Category.ShouldBe("Cat1");
        pagedList[1].Category.ShouldBe("Cat1");
        pagedList[2].Category.ShouldBe("Cat2");
    }

    [Test]
    public void WithPafiso_WithSorting_CustomMapping_SortsCorrectly() {
        // Arrange
        var products = new List<Product> {
            new() { Id = 1, Name = "Zebra", Category = "Cat1", Price = 10 },
            new() { Id = 2, Name = "Apple", Category = "Cat2", Price = 20 },
            new() { Id = 3, Name = "Banana", Category = "Cat1", Price = 30 }
        }.AsQueryable();

        var queryCollection = CreateQueryCollection(new Dictionary<string, string> {
            ["sortings[0][prop]"] = "ProductName",
            ["sortings[0][ord]"] = "asc"
        });

        // Act
        var result = products.WithPafiso(queryCollection, configure: opt => {
            opt.WithSorting<ProductSortDto>()
                .Map(dto => dto.ProductName, entity => entity.Name);
        });
        var pagedList = result.ToPagedList();

        // Assert
        pagedList.Count.ShouldBe(3);
        pagedList[0].Name.ShouldBe("Apple");
        pagedList[1].Name.ShouldBe("Banana");
        pagedList[2].Name.ShouldBe("Zebra");
    }

    [Test]
    public void WithPafiso_WithAllFeatures_WorksCorrectly() {
        // Arrange
        var products = new List<Product> {
            new() { Id = 1, Name = "Product A", Category = "Electronics", Price = 100 },
            new() { Id = 2, Name = "Product B", Category = "Electronics", Price = 200 },
            new() { Id = 3, Name = "Product C", Category = "Books", Price = 50 },
            new() { Id = 4, Name = "Product D", Category = "Electronics", Price = 150 },
            new() { Id = 5, Name = "Product E", Category = "Electronics", Price = 250 }
        }.AsQueryable();

        var queryCollection = CreateQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"] = "eq",
            ["filters[0][val]"] = "Electronics",
            ["sortings[0][prop]"] = "ProductName",
            ["sortings[0][ord]"] = "desc",
            ["skip"] = "0",
            ["take"] = "2"
        });

        // Act
        var result = products.WithPafiso(queryCollection, configure: opt => {
            opt.WithPaging();
            opt.WithFiltering<ProductFilterDto>();
            opt.WithSorting<ProductSortDto>()
                .Map(dto => dto.ProductName, entity => entity.Name);
        });
        var pagedList = result.ToPagedList();

        // Assert
        pagedList.TotalEntries.ShouldBe(4); // 4 Electronics items
        pagedList.Count.ShouldBe(2); // But only 2 per page
        pagedList[0].Name.ShouldBe("Product E"); // Sorted descending
        pagedList[1].Name.ShouldBe("Product D");
    }

    [Test]
    public void WithPafiso_CustomSettings_UsesSettings() {
        // Arrange
        var products = new List<Product> {
            new() { Id = 1, Name = "Test", Category = "cat1", Price = 10 },
            new() { Id = 2, Name = "Test2", Category = "CAT1", Price = 20 }
        }.AsQueryable();

        var queryCollection = CreateQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "category",
            ["filters[0][op]"] = "eq",
            ["filters[0][val]"] = "CAT1",
            ["filters[0][case]"] = "false"
        });

        var settings = new PafisoSettings {
            StringComparison = StringComparison.OrdinalIgnoreCase
        };

        // Act
        var result = products.WithPafiso(queryCollection, settings, configure: opt => {
            opt.WithFiltering<ProductFilterDto>();
        });
        var pagedList = result.ToPagedList();

        // Assert - both should match because of case-insensitive comparison
        pagedList.TotalEntries.ShouldBe(2);
    }

    [Test]
    public void WithPafiso_MultipleFilterConfigurations_CombinesFilters() {
        // Arrange
        var products = new List<Product> {
            new() { Id = 1, Name = "Product A", Category = "Electronics", Price = 10 },
            new() { Id = 2, Name = "Product B", Category = "Books", Price = 20 },
            new() { Id = 3, Name = "Product C", Category = "Electronics", Price = 30 }
        }.AsQueryable();

        var queryCollection = CreateQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"] = "eq",
            ["filters[0][val]"] = "Electronics",
            ["filters[1][fields]"] = "ProductId",
            ["filters[1][op]"] = "gt",
            ["filters[1][val]"] = "1"
        });

        // Act
        var result = products.WithPafiso(queryCollection, configure: opt => {
            opt.WithFiltering<ProductFilterDto>()
                .Map(dto => dto.ProductId, entity => entity.Id);
        });
        var pagedList = result.ToPagedList();

        // Assert - Should get product 3 (Electronics AND Id > 1)
        pagedList.Count.ShouldBe(1);
        pagedList[0].Id.ShouldBe(3);
    }
}
