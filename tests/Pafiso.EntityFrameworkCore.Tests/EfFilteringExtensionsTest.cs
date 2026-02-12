using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using Pafiso.AspNetCore;
using Pafiso.EntityFrameworkCore;
using Pafiso.Mapping;
using Shouldly;

namespace Pafiso.EntityFrameworkCore.Tests;

public class EfFilteringExtensionsTest {
    public class ProductSearchDto : MappingModel {
        public string? ProductName { get; set; }
        public string? Category { get; set; }
    }

    public class ProductSortDto : MappingModel {
        public string? ProductName { get; set; }
    }

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
    public void WithEfFiltering_CreatesFilterWithEfCoreExpressionBuilder() {
        // Arrange
        var queryCollection = CreateQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "ProductName",
            ["filters[0][op]"] = "contains",
            ["filters[0][val]"] = "Widget"
        });

        // Act - Use WithFiltering with EfCoreExpressionBuilder (what WithEfFiltering does)
        var searchParams = queryCollection.ToSearchParameters<Product>(builder => {
            builder.WithFiltering<ProductSearchDto>(EfCoreExpressionBuilder.Instance)
                .Map(dto => dto.ProductName, entity => entity.Name);
        });

        // Assert
        searchParams.Filters.Count.ShouldBe(1);
        searchParams.Filters[0].Fields.ShouldBe(["ProductName"]);
        searchParams.Filters[0].Operator.ShouldBe(FilterOperator.Contains);
        searchParams.Filters[0].Value.ShouldBe("Widget");
    }

    [Test]
    public void WithEfFiltering_WithCaseSensitiveSettings_UsesCaseSensitivity() {
        // Arrange
        var queryCollection = CreateQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"] = "eq",
            ["filters[0][val]"] = "Electronics"
        });

        // Act - WithEfFiltering with settings passes caseSensitive=true
        var searchParams = queryCollection.ToSearchParameters<Product>(builder => {
            builder.WithFiltering<ProductSearchDto>(EfCoreExpressionBuilder.Instance, true);
        });

        // Assert - The filter should have caseSensitive=true from the default
        searchParams.Filters.Count.ShouldBe(1);
        searchParams.Filters[0].CaseSensitive.ShouldBeTrue();
    }

    [Test]
    public void WithEfFiltering_DefaultCaseInsensitive() {
        // Arrange
        var queryCollection = CreateQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"] = "eq",
            ["filters[0][val]"] = "Electronics"
        });

        // Act - Default: case insensitive
        var searchParams = queryCollection.ToSearchParameters<Product>(builder => {
            builder.WithFiltering<ProductSearchDto>(EfCoreExpressionBuilder.Instance);
        });

        // Assert - Default EfFilteringSettings has CaseSensitive = false
        searchParams.Filters.Count.ShouldBe(1);
        searchParams.Filters[0].CaseSensitive.ShouldBeFalse();
    }

    [Test]
    public void WithEfFiltering_QueryOverridesCaseSensitivity() {
        // Arrange - query sets case=true, overriding default
        var queryCollection = CreateQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"] = "eq",
            ["filters[0][val]"] = "Electronics",
            ["filters[0][case]"] = "true"
        });

        var searchParams = queryCollection.ToSearchParameters<Product>(builder => {
            builder.WithFiltering<ProductSearchDto>(EfCoreExpressionBuilder.Instance);
        });

        searchParams.Filters[0].CaseSensitive.ShouldBeTrue();
    }

    [Test]
    public void WithEfFiltering_CanCombineWithSorting() {
        // Arrange
        var queryCollection = CreateQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"] = "eq",
            ["filters[0][val]"] = "Electronics",
            ["sortings[0][prop]"] = "ProductName",
            ["sortings[0][ord]"] = "asc"
        });

        // Act
        var searchParams = queryCollection.ToSearchParameters<Product>(builder => {
            builder.WithFiltering<ProductSearchDto>(EfCoreExpressionBuilder.Instance);
            builder.WithSorting<ProductSortDto>()
                .Map(dto => dto.ProductName, entity => entity.Name);
        });

        // Assert
        searchParams.Filters.Count.ShouldBe(1);
        searchParams.Sortings.Count.ShouldBe(1);
    }

    [Test]
    public void WithEfFiltering_ProducesSearchParametersWithCorrectConfiguration() {
        // EfCoreExpressionBuilder uses EF.Functions.Like which requires a real EF provider,
        // so we only verify the SearchParameters are configured correctly
        var queryCollection = CreateQueryCollection(new Dictionary<string, string> {
            ["filters[0][fields]"] = "Category",
            ["filters[0][op]"] = "eq",
            ["filters[0][val]"] = "Electronics",
            ["filters[1][fields]"] = "ProductName",
            ["filters[1][op]"] = "contains",
            ["filters[1][val]"] = "Widget"
        });

        var searchParams = queryCollection.ToSearchParameters<Product>(builder => {
            builder.WithFiltering<ProductSearchDto>(EfCoreExpressionBuilder.Instance)
                .Map(dto => dto.ProductName, entity => entity.Name);
        });

        // Assert - verify filter configuration is correct
        searchParams.Filters.Count.ShouldBe(2);
        searchParams.Filters[0].Fields.ShouldBe(["Category"]);
        searchParams.Filters[0].Operator.ShouldBe(FilterOperator.Equals);
        searchParams.Filters[0].Value.ShouldBe("Electronics");
        searchParams.Filters[1].Fields.ShouldBe(["ProductName"]);
        searchParams.Filters[1].Operator.ShouldBe(FilterOperator.Contains);
        searchParams.Filters[1].Value.ShouldBe("Widget");
    }

    [Test]
    public void EfCoreExpressionBuilder_BuildFilterExpression_WorksWithContains() {
        // Test that EfCoreExpressionBuilder builds a valid expression for string contains
        var builder = EfCoreExpressionBuilder.Instance;
        var settings = new PafisoSettings { UseEfCoreLikeForCaseInsensitive = true };

        // This should not throw
        var expr = builder.BuildFilterExpression<Product>(
            "Name", "x", FilterOperator.Contains, "test", false, settings);

        expr.ShouldNotBeNull();
        expr.ReturnType.ShouldBe(typeof(bool));
    }

    [Test]
    public void EfCoreExpressionBuilder_BuildFilterExpression_WorksWithEquals() {
        var builder = EfCoreExpressionBuilder.Instance;
        var settings = new PafisoSettings { UseEfCoreLikeForCaseInsensitive = true };

        var expr = builder.BuildFilterExpression<Product>(
            "Name", "x", FilterOperator.Equals, "test", false, settings);

        expr.ShouldNotBeNull();
        expr.ReturnType.ShouldBe(typeof(bool));
    }

    [Test]
    public void EfFilteringSettings_DefaultsCaseInsensitive() {
        var settings = new EfFilteringSettings();
        settings.CaseSensitive.ShouldBeFalse();
    }

    [Test]
    public void EfFilteringSettings_CanSetCaseSensitive() {
        var settings = new EfFilteringSettings { CaseSensitive = true };
        settings.CaseSensitive.ShouldBeTrue();
    }
}
