using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using Pafiso.AspNetCore;
using Shouldly;

namespace Pafiso.AspNetCore.Tests;

public class SearchParametersExtensionsTest {
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
    public void ToSearchParameters_WithConfiguration_CreatesValidSearchParameters() {
        // Arrange
        var queryCollection = CreateQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "ProductId",
            ["filters[0][op]"] = "eq",
            ["filters[0][val]"] = "2",
            ["sortings[0][prop]"] = "ProductName",
            ["sortings[0][ord]"] = "asc",
            ["skip"] = "0",
            ["take"] = "10"
        });

        // Act
        var searchParams = queryCollection.ToSearchParameters<Product>(builder => {
            builder.WithPaging();
            builder.WithFiltering<ProductFilterDto>()
                .Map(dto => dto.ProductId, entity => entity.Id);
            builder.WithSorting<ProductSortDto>()
                .Map(dto => dto.ProductName, entity => entity.Name);
        });

        // Assert
        searchParams.ShouldNotBeNull();
        searchParams.Filters.Count.ShouldBe(1);
        searchParams.Sortings.Count.ShouldBe(1);
        searchParams.Paging.ShouldNotBeNull();
        searchParams.Paging!.Skip.ShouldBe(0);
        searchParams.Paging.Take.ShouldBe(10);
    }

    [Test]
    public void ToSearchParameters_WithOnlyFiltering_CreatesValidSearchParameters() {
        // Arrange
        var queryCollection = CreateQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"] = "eq",
            ["filters[0][val]"] = "Electronics"
        });

        // Act
        var searchParams = queryCollection.ToSearchParameters<Product>(builder => {
            builder.WithFiltering<ProductFilterDto>();
        });

        // Assert
        searchParams.Filters.Count.ShouldBe(1);
        searchParams.Sortings.Count.ShouldBe(0);
        searchParams.Paging.ShouldBeNull();
    }

    [Test]
    public void WithPafiso_UsingSearchParameters_AppliesCorrectly() {
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
            ["skip"] = "0",
            ["take"] = "2"
        });

        var searchParams = queryCollection.ToSearchParameters<Product>(builder => {
            builder.WithPaging();
            builder.WithFiltering<ProductFilterDto>();
        });

        // Act
        var result = products.WithPafiso(searchParams);
        var pagedList = result.ToPagedList();

        // Assert
        pagedList.TotalEntries.ShouldBe(2);
        pagedList.Count.ShouldBe(2);
        pagedList.ShouldAllBe(p => p.Category == "Electronics");
    }

    [Test]
    public void WithPafiso_UsingSearchParameters_WithCustomMapping_WorksCorrectly() {
        // Arrange
        var products = new List<Product> {
            new() { Id = 1, Name = "Zebra", Category = "Cat1", Price = 10 },
            new() { Id = 2, Name = "Apple", Category = "Cat2", Price = 20 },
            new() { Id = 3, Name = "Banana", Category = "Cat1", Price = 30 }
        }.AsQueryable();

        var queryCollection = CreateQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "ProductId",
            ["filters[0][op]"] = "gt",
            ["filters[0][val]"] = "1",
            ["sortings[0][prop]"] = "ProductName",
            ["sortings[0][ord]"] = "asc"
        });

        var searchParams = queryCollection.ToSearchParameters<Product>(builder => {
            builder.WithFiltering<ProductFilterDto>()
                .Map(dto => dto.ProductId, entity => entity.Id);
            builder.WithSorting<ProductSortDto>()
                .Map(dto => dto.ProductName, entity => entity.Name);
        });

        // Act
        var result = products.WithPafiso(searchParams);
        var pagedList = result.ToPagedList();

        // Assert
        pagedList.Count.ShouldBe(2);
        pagedList[0].Name.ShouldBe("Apple"); // Sorted alphabetically
        pagedList[1].Name.ShouldBe("Banana");
    }

    [Test]
    public void ToSearchParameters_WithoutConfiguration_OnlyReturnsPaging() {
        // Arrange
        var queryCollection = CreateQueryCollection(new Dictionary<string, string> {
            ["skip"] = "10",
            ["take"] = "20"
        });

        // Act
        var searchParams = queryCollection.ToSearchParameters();

        // Assert
        searchParams.ShouldNotBeNull();
        searchParams.Paging.ShouldNotBeNull();
        searchParams.Paging!.Skip.ShouldBe(10);
        searchParams.Paging.Take.ShouldBe(20);
        searchParams.Filters.Count.ShouldBe(0);
        searchParams.Sortings.Count.ShouldBe(0);
    }

    [Test]
    public void WithPafiso_UsingSearchParameters_WithSettings_UsesSettings() {
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

        var searchParams = queryCollection.ToSearchParameters<Product>(builder => {
            builder.WithFiltering<ProductFilterDto>();
        }, settings);

        // Act
        var result = products.WithPafiso(searchParams, settings);
        var pagedList = result.ToPagedList();

        // Assert - both should match because of case-insensitive comparison
        pagedList.TotalEntries.ShouldBe(2);
    }

    [Test]
    public void WithPafiso_UsingSearchParametersAndBuilder_BothWork() {
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

        // Act - Using SearchParameters
        var searchParams = queryCollection.ToSearchParameters<Product>(builder => {
            builder.WithFiltering<ProductFilterDto>();
        });
        var result1 = products.WithPafiso(searchParams);
        var pagedList1 = result1.ToPagedList();

        // Act - Using builder directly
        var result2 = products.WithPafiso(queryCollection, configure: opt => {
            opt.WithFiltering<ProductFilterDto>();
        });
        var pagedList2 = result2.ToPagedList();

        // Assert - Both should give same results
        pagedList1.TotalEntries.ShouldBe(pagedList2.TotalEntries);
        pagedList1.Count.ShouldBe(pagedList2.Count);
        pagedList1.ShouldAllBe(p => p.Category == "Electronics");
        pagedList2.ShouldAllBe(p => p.Category == "Electronics");
    }

    [Test]
    public void SearchParametersBuilder_MultipleFilterConfigurations_WorksCorrectly() {
        // Arrange
        var queryCollection = CreateQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"] = "eq",
            ["filters[0][val]"] = "Electronics",
            ["filters[1][fields]"] = "ProductId",
            ["filters[1][op]"] = "gt",
            ["filters[1][val]"] = "1",
            ["sortings[0][prop]"] = "ProductName",
            ["sortings[0][ord]"] = "desc"
        });

        // Act
        var searchParams = queryCollection.ToSearchParameters<Product>(builder => {
            builder.WithFiltering<ProductFilterDto>()
                .Map(dto => dto.ProductId, entity => entity.Id);
            builder.WithSorting<ProductSortDto>()
                .Map(dto => dto.ProductName, entity => entity.Name);
        });

        // Assert
        searchParams.Filters.Count.ShouldBe(2);
        searchParams.Sortings.Count.ShouldBe(1);
    }
}
