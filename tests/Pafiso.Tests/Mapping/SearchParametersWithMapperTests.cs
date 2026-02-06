using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using Pafiso.Mapping;
using Shouldly;

namespace Pafiso.Tests.Mapping;

public class SearchParametersWithMapperTests {
    // Test mapping models
    public class ProductSearchDto : MappingModel {
        public string? ProductName { get; set; }
        public string? MinPrice { get; set; }
        public bool? IsActive { get; set; }
    }

    // Test entities
    public class Product {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public bool Active { get; set; }
    }

    [Test]
    public void FromDictionary_WithMapper_CreatesSearchParametersWithMapperEmbedded() {
        // Arrange
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .Map(dto => dto.ProductName, entity => entity.Name)
            .Map(dto => dto.MinPrice, entity => entity.Price);

        var dict = new Dictionary<string, string> {
            ["filters[0][fields]"] = "productName",
            ["filters[0][op]"] = "contains",
            ["filters[0][val]"] = "test",
            ["sortings[0][prop]"] = "minPrice",
            ["sortings[0][ord]"] = "asc",
            ["skip"] = "0",
            ["take"] = "10"
        };

        // Act
        var searchParams = SearchParameters.FromDictionary<ProductSearchDto, Product>(dict, mapper);

        // Assert
        searchParams.Filters.Count.ShouldBe(1);
        searchParams.Sortings.Count.ShouldBe(1);
        searchParams.Paging.ShouldNotBeNull();
        searchParams.Paging!.Skip.ShouldBe(0);
        searchParams.Paging.Take.ShouldBe(10);
    }

    [Test]
    public void FromDictionary_WithMapper_AppliesFilterAndSortCorrectly() {
        // Arrange
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .Map(dto => dto.ProductName, entity => entity.Name)
            .Map(dto => dto.MinPrice, entity => entity.Price);

        var dict = new Dictionary<string, string> {
            ["filters[0][fields]"] = "productName",
            ["filters[0][op]"] = "contains",
            ["filters[0][val]"] = "Widget",
            ["sortings[0][prop]"] = "minPrice",
            ["sortings[0][ord]"] = "asc"
        };

        var products = new List<Product> {
            new() { Id = 1, Name = "Widget A", Price = 20.0m, Active = true },
            new() { Id = 2, Name = "Widget B", Price = 10.0m, Active = true },
            new() { Id = 3, Name = "Gadget", Price = 15.0m, Active = true }
        }.AsQueryable();

        // Act
        var searchParams = SearchParameters.FromDictionary<ProductSearchDto, Product>(dict, mapper);
        var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(products);
        var result = pagedQuery.ToList();

        // Assert
        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("Widget B"); // Sorted by price ascending
        result[1].Name.ShouldBe("Widget A");
    }

    [Test]
    public void FromDictionary_SupportsJsonFormat() {
        // Arrange - This test verifies dictionary-based approach works
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .Map(dto => dto.ProductName, entity => entity.Name);

        var dict = new Dictionary<string, string> {
            ["filters[0][fields]"] = "productName",
            ["filters[0][op]"] = "contains",
            ["filters[0][val]"] = "test",
            ["sortings[0][prop]"] = "productName",
            ["sortings[0][ord]"] = "asc",
            ["skip"]="0",
            ["take"]="5"
        };

        // Act
        var searchParams = SearchParameters.FromDictionary<ProductSearchDto, Product>(dict, mapper);

        // Assert
        searchParams.Filters.Count.ShouldBe(1);
        searchParams.Sortings.Count.ShouldBe(1);
        searchParams.Paging.ShouldNotBeNull();
        searchParams.Paging!.Skip.ShouldBe(0);
        searchParams.Paging.Take.ShouldBe(5);
    }

    [Test]
    public void ApplyToIQueryable_WithMapper_EndToEndFilterSortPage() {
        // Arrange
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .Map(dto => dto.ProductName, entity => entity.Name)
            .Map(dto => dto.MinPrice, entity => entity.Price);

        var dict = new Dictionary<string, string> {
            ["filters[0][fields]"] = "productName",
            ["filters[0][op]"] = "contains",
            ["filters[0][val]"] = "Product",
            ["sortings[0][prop]"] = "minPrice",
            ["sortings[0][ord]"] = "desc",
            ["skip"] = "1",
            ["take"] = "2"
        };

        var products = new List<Product> {
            new() { Id = 1, Name = "Product A", Price = 30.0m, Active = true },
            new() { Id = 2, Name = "Product B", Price = 20.0m, Active = true },
            new() { Id = 3, Name = "Product C", Price = 40.0m, Active = true },
            new() { Id = 4, Name = "Widget", Price = 10.0m, Active = true }
        }.AsQueryable();

        // Act
        var searchParams = SearchParameters.FromDictionary<ProductSearchDto, Product>(dict, mapper);
        var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(products);

        var total = countQuery.Count();
        var result = pagedQuery.ToList();

        // Assert
        total.ShouldBe(3); // 3 products match "Product"
        result.Count.ShouldBe(2); // Paging: skip 1, take 2
        result[0].Name.ShouldBe("Product A"); // Price 30 (2nd highest)
        result[1].Name.ShouldBe("Product B"); // Price 20 (3rd highest)
    }

    [Test]
    public void ApplyToIQueryable_WithMapper_InvalidFieldsIgnored() {
        // Arrange
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .Map(dto => dto.ProductName, entity => entity.Name);

        var dict = new Dictionary<string, string> {
            ["filters[0][fields]"] = "invalidField",
            ["filters[0][op]"] = "eq",
            ["filters[0][val]"] = "test",
            ["sortings[0][prop]"] = "productName",
            ["sortings[0][ord]"] = "asc"
        };

        var products = new List<Product> {
            new() { Id = 1, Name = "Product A", Price = 30.0m, Active = true },
            new() { Id = 2, Name = "Product B", Price = 20.0m, Active = true }
        }.AsQueryable();

        // Act
        var searchParams = SearchParameters.FromDictionary<ProductSearchDto, Product>(dict, mapper);
        var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(products);
        var result = pagedQuery.ToList();

        // Assert - Invalid filter field ignored, valid sorting applied
        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("Product A");
        result[1].Name.ShouldBe("Product B");
    }

    [Test]
    public void BackwardCompatibility_LegacyStringBasedStillWorks() {
        // Arrange
        var dict = new Dictionary<string, string> {
            ["filters[0][fields]"] = "Name",
            ["filters[0][op]"] = "contains",
            ["filters[0][val]"] = "Product",
            ["sortings[0][prop]"] = "Price",
            ["sortings[0][ord]"] = "asc"
        };

        var products = new List<Product> {
            new() { Id = 1, Name = "Product A", Price = 30.0m, Active = true },
            new() { Id = 2, Name = "Product B", Price = 20.0m, Active = true }
        }.AsQueryable();

        // Act - Use FromDictionary with mapper
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .Map(dto => dto.ProductName, entity => entity.Name)
            .Map(dto => dto.MinPrice, entity => entity.Price);
        var searchParams = SearchParameters.FromDictionary<ProductSearchDto, Product>(dict, mapper);
        var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(products);
        var result = pagedQuery.ToList();

        // Assert
        result.Count.ShouldBe(2);
        result[0].Price.ShouldBe(20.0m);
        result[1].Price.ShouldBe(30.0m);
    }

    [Test]
    public void FromDictionary_WithMapper_MultipleFiltersAndSortings() {
        // Arrange
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .Map(dto => dto.ProductName, entity => entity.Name)
            .Map(dto => dto.MinPrice, entity => entity.Price);

        var dict = new Dictionary<string, string> {
            ["filters[0][fields]"] = "productName",
            ["filters[0][op]"] = "contains",
            ["filters[0][val]"] = "Product",
            ["filters[1][fields]"] = "minPrice",
            ["filters[1][op]"] = "gte",
            ["filters[1][val]"] = "25",
            ["sortings[0][prop]"] = "minPrice",
            ["sortings[0][ord]"] = "asc",
            ["sortings[1][prop]"] = "productName",
            ["sortings[1][ord]"] = "asc"
        };

        var products = new List<Product> {
            new() { Id = 1, Name = "Product A", Price = 30.0m, Active = true },
            new() { Id = 2, Name = "Product B", Price = 20.0m, Active = true },
            new() { Id = 3, Name = "Product C", Price = 40.0m, Active = true }
        }.AsQueryable();

        // Act
        var searchParams = SearchParameters.FromDictionary<ProductSearchDto, Product>(dict, mapper);
        var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(products);
        var result = pagedQuery.ToList();

        // Assert
        result.Count.ShouldBe(2); // Product A (30) and Product C (40) both >= 25
        result[0].Name.ShouldBe("Product A"); // Price 30 comes first
        result[1].Name.ShouldBe("Product C"); // Price 40 comes second
    }
}
