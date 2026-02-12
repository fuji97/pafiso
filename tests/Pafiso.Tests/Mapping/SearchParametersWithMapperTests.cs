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
    public void FromJson_WithNoFilters_CreatesSearchParametersWithSortingsAndPaging() {
        // Arrange
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .Map(dto => dto.ProductName, entity => entity.Name)
            .Map(dto => dto.MinPrice, entity => entity.Price);

        // JSON with sortings and paging only (no filters)
        var json = """
        {
            "Filters": [],
            "Sortings": [{"PropertyName": "minPrice", "SortOrder": 0}],
            "Paging": {"Take": 10, "Skip": 0}
        }
        """;

        // Act
        var searchParams = SearchParameters.FromJson<ProductSearchDto, Product>(json, mapper);

        // Assert
        searchParams.Filters.ShouldBeEmpty();
        searchParams.Sortings.Count.ShouldBe(1);
        searchParams.Paging.ShouldNotBeNull();
    }

    [Test]
    public void FromJson_WithEmptyJson_ReturnsEmptySearchParameters() {
        var mapper = new FieldMapper<ProductSearchDto, Product>();
        var json = "{}";

        var searchParams = SearchParameters.FromJson<ProductSearchDto, Product>(json, mapper);

        searchParams.Filters.ShouldBeEmpty();
        searchParams.Sortings.ShouldBeEmpty();
        searchParams.Paging.ShouldBeNull();
    }

    [Test]
    public void FromJson_WithSortingsOnly_AppliesCorrectly() {
        // Arrange
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .Map(dto => dto.MinPrice, entity => entity.Price);

        var json = """
        {
            "Filters": [],
            "Sortings": [{"PropertyName": "minPrice", "SortOrder": 0}]
        }
        """;

        var products = new List<Product> {
            new() { Id = 1, Name = "Widget A", Price = 20.0m, Active = true },
            new() { Id = 2, Name = "Widget B", Price = 10.0m, Active = true },
            new() { Id = 3, Name = "Gadget", Price = 15.0m, Active = true }
        }.AsQueryable();

        // Act
        var searchParams = SearchParameters.FromJson<ProductSearchDto, Product>(json, mapper);
        var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(products);
        var result = pagedQuery.ToList();

        // Assert
        result.Count.ShouldBe(3);
        result[0].Price.ShouldBe(10.0m);
        result[1].Price.ShouldBe(15.0m);
        result[2].Price.ShouldBe(20.0m);
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

    [Test]
    public void OperatorPlus_CombinesSearchParameters() {
        // Arrange
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .Map(dto => dto.ProductName, entity => entity.Name)
            .Map(dto => dto.MinPrice, entity => entity.Price);

        var sp1 = new SearchParameters {
            Filters = [Filter.WithMapper("productName", FilterOperator.Contains, "Widget", mapper)],
            Paging = Paging.FromSkipTake(0, 10)
        };

        var sp2 = new SearchParameters {
            Sortings = [Sorting.WithMapper("minPrice", SortOrder.Ascending, mapper)]
        };

        // Act
        var combined = sp1 + sp2;

        // Assert
        combined.Filters.Count.ShouldBe(1);
        combined.Sortings.Count.ShouldBe(1);
        combined.Paging.ShouldNotBeNull();
        combined.Paging!.Skip.ShouldBe(0);
        combined.Paging.Take.ShouldBe(10);
    }

    [Test]
    public void OperatorPlus_LeftPagingTakesPrecedence() {
        var sp1 = new SearchParameters { Paging = Paging.FromSkipTake(0, 5) };
        var sp2 = new SearchParameters { Paging = Paging.FromSkipTake(10, 20) };

        var combined = sp1 + sp2;

        combined.Paging.ShouldNotBeNull();
        combined.Paging!.Skip.ShouldBe(0);
        combined.Paging.Take.ShouldBe(5);
    }

    [Test]
    public void ToDictionary_RoundTrips_WithMapper() {
        // Arrange
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .Map(dto => dto.ProductName, entity => entity.Name)
            .Map(dto => dto.MinPrice, entity => entity.Price);

        var dict = new Dictionary<string, string> {
            ["filters[0][fields]"] = "productName",
            ["filters[0][op]"] = "contains",
            ["filters[0][val]"] = "Widget",
            ["sortings[0][prop]"] = "minPrice",
            ["sortings[0][ord]"] = "asc",
            ["skip"] = "0",
            ["take"] = "10"
        };

        var original = SearchParameters.FromDictionary<ProductSearchDto, Product>(dict, mapper);

        // Act
        var serialized = original.ToDictionary();

        // Assert
        serialized.ShouldContainKey("filters[0][fields]");
        serialized.ShouldContainKey("filters[0][op]");
        serialized.ShouldContainKey("filters[0][val]");
        serialized.ShouldContainKey("sortings[0][prop]");
        serialized.ShouldContainKey("sortings[0][ord]");
        serialized.ShouldContainKey("skip");
        serialized.ShouldContainKey("take");
        serialized["skip"].ShouldBe("0");
        serialized["take"].ShouldBe("10");
    }

    [Test]
    public void Equality_SameSearchParameters_AreEqual() {
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .Map(dto => dto.ProductName, entity => entity.Name);

        var dict = new Dictionary<string, string> {
            ["filters[0][fields]"] = "productName",
            ["filters[0][op]"] = "contains",
            ["filters[0][val]"] = "Widget",
            ["sortings[0][prop]"] = "productName",
            ["sortings[0][ord]"] = "asc",
            ["skip"] = "0",
            ["take"] = "10"
        };

        var sp1 = SearchParameters.FromDictionary<ProductSearchDto, Product>(dict, mapper);
        var sp2 = SearchParameters.FromDictionary<ProductSearchDto, Product>(dict, mapper);

        (sp1 == sp2).ShouldBeTrue();
    }
}
