using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using Pafiso.Extensions;
using Shouldly;

namespace Pafiso.Tests;

public class SortingWithSettingsTest {
    private class Product {
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }

    private class ProductWithJsonAttributes {
        [JsonPropertyName("product_name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("unit_price")]
        public decimal Price { get; set; }

        [JsonPropertyName("stock_quantity")]
        public int Stock { get; set; }
    }

    private List<Product> _products = null!;
    private List<ProductWithJsonAttributes> _productsWithAttributes = null!;

    [SetUp]
    public void Setup() {
        _products = [
            new Product { Name = "Apple", Price = 1.50m, Stock = 100 },
            new Product { Name = "Banana", Price = 0.75m, Stock = 150 },
            new Product { Name = "Cherry", Price = 3.00m, Stock = 50 },
            new Product { Name = "Date", Price = 2.25m, Stock = 75 }
        ];

        _productsWithAttributes = [
            new ProductWithJsonAttributes { Name = "Apple", Price = 1.50m, Stock = 100 },
            new ProductWithJsonAttributes { Name = "Banana", Price = 0.75m, Stock = 150 },
            new ProductWithJsonAttributes { Name = "Cherry", Price = 3.00m, Stock = 50 }
        ];

        PafisoSettings.Default = new PafisoSettings();
    }

    [TearDown]
    public void TearDown() {
        PafisoSettings.Default = new PafisoSettings();
    }

    #region Field Name Resolution Tests

    [Test]
    public void ApplyToIQueryable_WithCamelCaseNamingPolicy_ResolvesFieldNames() {
        var settings = new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var sorting = new Sorting("name", SortOrder.Ascending);

        var sorted = sorting.ApplyToIQueryable(_products.AsQueryable(), settings).ToList();

        sorted[0].Name.ShouldBe("Apple");
        sorted[1].Name.ShouldBe("Banana");
        sorted[2].Name.ShouldBe("Cherry");
        sorted[3].Name.ShouldBe("Date");
    }

    [Test]
    public void ApplyToIQueryable_WithJsonPropertyNameAttribute_ResolvesFieldNames() {
        var settings = new PafisoSettings {
            UseJsonPropertyNameAttributes = true
        };

        var sorting = new Sorting("product_name", SortOrder.Descending);

        var sorted = sorting.ApplyToIQueryable(_productsWithAttributes.AsQueryable(), settings).ToList();

        sorted[0].Name.ShouldBe("Cherry");
        sorted[1].Name.ShouldBe("Banana");
        sorted[2].Name.ShouldBe("Apple");
    }

    [Test]
    public void ApplyToIQueryable_WithJsonPropertyName_NumericSort() {
        var settings = new PafisoSettings {
            UseJsonPropertyNameAttributes = true
        };

        var sorting = new Sorting("unit_price", SortOrder.Ascending);

        var sorted = sorting.ApplyToIQueryable(_productsWithAttributes.AsQueryable(), settings).ToList();

        sorted[0].Price.ShouldBe(0.75m); // Banana
        sorted[1].Price.ShouldBe(1.50m); // Apple
        sorted[2].Price.ShouldBe(3.00m); // Cherry
    }

    #endregion

    #region ThenApplyToIQueryable Tests

    [Test]
    public void ThenApplyToIQueryable_WithSettings_WorksCorrectly() {
        var settings = new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var primarySort = new Sorting("price", SortOrder.Ascending);
        var secondarySort = new Sorting("name", SortOrder.Ascending);

        var sorted = primarySort.ApplyToIQueryable(_products.AsQueryable(), settings);
        sorted = secondarySort.ThenApplyToIQueryable(sorted, settings);

        var result = sorted.ToList();

        // First by price, then by name
        result[0].Name.ShouldBe("Banana"); // 0.75
        result[1].Name.ShouldBe("Apple");  // 1.50
        result[2].Name.ShouldBe("Date");   // 2.25
        result[3].Name.ShouldBe("Cherry"); // 3.00
    }

    [Test]
    public void ThenApplyToIQueryable_WithRestrictionsAndSettings_RespectsRestrictions() {
        var settings = new PafisoSettings {
            UseJsonPropertyNameAttributes = true
        };

        var restrictions = new FieldRestrictions()
            .AllowSorting("Name", "Price");

        var primarySort = new Sorting("product_name", SortOrder.Ascending);
        var secondarySort = new Sorting("stock_quantity", SortOrder.Descending);

        var sorted = primarySort.ApplyToIQueryable(_productsWithAttributes.AsQueryable(), restrictions, settings);
        sorted.ShouldNotBeNull();

        // Secondary sort on Stock should be ignored (not in allowed list)
        var result = secondarySort.ThenApplyToIQueryable(sorted!, restrictions, settings);

        // Result should just be sorted by Name
        result.ToList()[0].Name.ShouldBe("Apple");
    }

    #endregion

    #region Extension Methods with Settings Tests

    [Test]
    public void OrderByExtension_WithSettings_AppliesFieldNameResolution() {
        var settings = new PafisoSettings {
            UseJsonPropertyNameAttributes = true
        };

        var sorting = new Sorting("stock_quantity", SortOrder.Descending);

        var sorted = _productsWithAttributes.AsQueryable().OrderBy(sorting, settings).ToList();

        sorted[0].Stock.ShouldBe(150); // Banana
        sorted[1].Stock.ShouldBe(100); // Apple
        sorted[2].Stock.ShouldBe(50);  // Cherry
    }

    [Test]
    public void OrderByExtension_WithRestrictionsAndSettings_WorksTogether() {
        var settings = new PafisoSettings {
            UseJsonPropertyNameAttributes = true
        };

        var restrictions = new FieldRestrictions()
            .AllowSorting("Name");

        var sorting = new Sorting("product_name", SortOrder.Ascending);

        var sorted = _productsWithAttributes.AsQueryable().OrderBy(sorting, restrictions, settings);

        sorted.ShouldNotBeNull();
        sorted!.ToList()[0].Name.ShouldBe("Apple");
    }

    [Test]
    public void OrderByExtension_OnIEnumerable_WithSettings() {
        var settings = new PafisoSettings {
            UseJsonPropertyNameAttributes = true
        };

        var sorting = new Sorting("unit_price", SortOrder.Descending);

        var sorted = _productsWithAttributes.OrderBy(sorting, settings).ToList();

        sorted[0].Price.ShouldBe(3.00m);  // Cherry
        sorted[1].Price.ShouldBe(1.50m);  // Apple
        sorted[2].Price.ShouldBe(0.75m);  // Banana
    }

    [Test]
    public void ThenByExtension_WithSettings_WorksCorrectly() {
        var settings = new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var primarySort = new Sorting("price", SortOrder.Ascending);
        var secondarySort = new Sorting("name", SortOrder.Ascending);

        var sorted = _products.AsQueryable()
            .OrderBy(primarySort, settings)
            .ThenBy(secondarySort, settings)
            .ToList();

        sorted[0].Name.ShouldBe("Banana");
        sorted[3].Name.ShouldBe("Cherry");
    }

    #endregion

    #region Restrictions with Settings Tests

    [Test]
    public void ApplyToIQueryable_BlockedField_ReturnsNull() {
        var settings = new PafisoSettings {
            UseJsonPropertyNameAttributes = true
        };

        var restrictions = new FieldRestrictions()
            .BlockSorting("Price");

        var sorting = new Sorting("unit_price", SortOrder.Ascending);

        var result = sorting.ApplyToIQueryable(_productsWithAttributes.AsQueryable(), restrictions, settings);

        // Price is blocked after resolution, so should return null
        result.ShouldBeNull();
    }

    #endregion
}
