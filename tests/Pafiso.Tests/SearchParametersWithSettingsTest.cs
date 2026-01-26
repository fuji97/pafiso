using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using Shouldly;

namespace Pafiso.Tests;

public class SearchParametersWithSettingsTest {
    private class Order {
        public int Id { get; set; }
        public string CustomerName { get; set; } = null!;
        public decimal Total { get; set; }
        public string Status { get; set; } = null!;
    }

    private class OrderWithJsonAttributes {
        [JsonPropertyName("order_id")]
        public int Id { get; set; }

        [JsonPropertyName("customer_name")]
        public string CustomerName { get; set; } = null!;

        [JsonPropertyName("order_total")]
        public decimal Total { get; set; }

        [JsonPropertyName("order_status")]
        public string Status { get; set; } = null!;
    }

    private List<Order> _orders = null!;
    private List<OrderWithJsonAttributes> _ordersWithAttributes = null!;

    [SetUp]
    public void Setup() {
        _orders = [
            new Order { Id = 1, CustomerName = "Alice", Total = 150.00m, Status = "Pending" },
            new Order { Id = 2, CustomerName = "Bob", Total = 200.00m, Status = "Shipped" },
            new Order { Id = 3, CustomerName = "Charlie", Total = 75.00m, Status = "Pending" },
            new Order { Id = 4, CustomerName = "Diana", Total = 300.00m, Status = "Delivered" },
            new Order { Id = 5, CustomerName = "Eve", Total = 125.00m, Status = "Shipped" }
        ];

        _ordersWithAttributes = [
            new OrderWithJsonAttributes { Id = 1, CustomerName = "Alice", Total = 150.00m, Status = "Pending" },
            new OrderWithJsonAttributes { Id = 2, CustomerName = "Bob", Total = 200.00m, Status = "Shipped" },
            new OrderWithJsonAttributes { Id = 3, CustomerName = "Charlie", Total = 75.00m, Status = "Pending" },
            new OrderWithJsonAttributes { Id = 4, CustomerName = "Diana", Total = 300.00m, Status = "Delivered" }
        ];

        PafisoSettings.Default = new PafisoSettings();
    }

    [TearDown]
    public void TearDown() {
        PafisoSettings.Default = new PafisoSettings();
    }

    #region ApplyToIQueryable with Settings Tests

    [Test]
    public void ApplyToIQueryable_WithSettings_ResolvesFilterFieldNames() {
        var settings = new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var searchParams = new SearchParameters()
            .AddFilters(new Filter("customerName", FilterOperator.Contains, "a"));

        var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(_orders.AsQueryable(), settings);

        var results = pagedQuery.ToList();
        results.Count.ShouldBe(3); // Alice, Charlie, Diana
        countQuery.Count().ShouldBe(3);
    }

    [Test]
    public void ApplyToIQueryable_WithSettings_ResolvesSortingFieldNames() {
        var settings = new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var searchParams = new SearchParameters()
            .AddSorting(new Sorting("total", SortOrder.Descending));

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(_orders.AsQueryable(), settings);

        var results = pagedQuery.ToList();
        results[0].Total.ShouldBe(300.00m); // Diana
        results[4].Total.ShouldBe(75.00m);  // Charlie
    }

    [Test]
    public void ApplyToIQueryable_WithJsonPropertyNames_ResolvesCorrectly() {
        var settings = new PafisoSettings {
            UseJsonPropertyNameAttributes = true
        };

        var searchParams = new SearchParameters()
            .AddFilters(new Filter("order_status", FilterOperator.Equals, "pending"))
            .AddSorting(new Sorting("order_total", SortOrder.Ascending));

        var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(_ordersWithAttributes.AsQueryable(), settings);

        var results = pagedQuery.ToList();
        results.Count.ShouldBe(2); // Alice and Charlie
        results[0].CustomerName.ShouldBe("Charlie"); // Sorted by Total ascending (75 < 150)
        results[1].CustomerName.ShouldBe("Alice");
    }

    [Test]
    public void ApplyToIQueryable_WithPagingAndSettings_WorksTogether() {
        var settings = new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var searchParams = new SearchParameters(Paging.FromSkipTake(1, 2))
            .AddSorting(new Sorting("id", SortOrder.Ascending));

        var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(_orders.AsQueryable(), settings);

        var results = pagedQuery.ToList();
        results.Count.ShouldBe(2);
        results[0].Id.ShouldBe(2); // Skip 1, so starts at Id=2
        results[1].Id.ShouldBe(3);
        countQuery.Count().ShouldBe(5); // Total count before paging
    }

    #endregion

    #region ApplyToIQueryable with Restrictions and Settings Tests

    [Test]
    public void ApplyToIQueryable_WithRestrictionsAndSettings_RespectsRestrictions() {
        var settings = new PafisoSettings {
            UseJsonPropertyNameAttributes = true
        };

        var searchParams = new SearchParameters()
            .AddFilters(new Filter("customer_name", FilterOperator.Contains, "a"))
            .AddFilters(new Filter("order_total", FilterOperator.GreaterThan, "100"));

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _ordersWithAttributes.AsQueryable(),
            r => r.AllowFiltering("CustomerName"), // Only allow CustomerName
            settings);

        var results = pagedQuery.ToList();
        // order_total filter should be ignored, only customer_name applied
        results.Count.ShouldBe(3); // Alice, Charlie, Diana (all contain 'a')
    }

    [Test]
    public void ApplyToIQueryable_WithRestrictionsInstance_AndSettings_WorksTogether() {
        var settings = new PafisoSettings {
            UseJsonPropertyNameAttributes = true
        };

        var restrictions = new FieldRestrictions()
            .AllowFiltering("CustomerName", "Status")
            .AllowSorting("Total");

        var searchParams = new SearchParameters()
            .AddFilters(new Filter("order_status", FilterOperator.Equals, "shipped"))
            .AddSorting(new Sorting("order_total", SortOrder.Descending));

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _ordersWithAttributes.AsQueryable(),
            restrictions,
            settings);

        var results = pagedQuery.ToList();
        results.Count.ShouldBe(1); // Bob
        results[0].CustomerName.ShouldBe("Bob");
    }

    #endregion

    #region FromDictionary with Settings Tests

    [Test]
    public void FromDictionary_WithSettings_PreservesOriginalFieldNames() {
        var dict = new Dictionary<string, string> {
            ["filters[0][fields]"] = "customer_name",
            ["filters[0][op]"] = "contains",
            ["filters[0][val]"] = "alice"
        };

        var settings = new PafisoSettings {
            UseJsonPropertyNameAttributes = true
        };

        var searchParams = SearchParameters.FromDictionary(dict, settings);

        // Original field names are preserved in the SearchParameters
        searchParams.Filters.Count.ShouldBe(1);
        searchParams.Filters[0].Fields[0].ShouldBe("customer_name");

        // Resolution happens during ApplyToIQueryable
        var (_, pagedQuery) = searchParams.ApplyToIQueryable(_ordersWithAttributes.AsQueryable(), settings);
        pagedQuery.Count().ShouldBe(1);
    }

    #endregion

    #region Multiple Filters and Sortings Tests

    [Test]
    public void ApplyToIQueryable_MultipleFiltersAndSortings_WithSettings() {
        var settings = new PafisoSettings {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var searchParams = new SearchParameters()
            .AddFilters(
                new Filter("status", FilterOperator.NotEquals, "delivered"),
                new Filter("total", FilterOperator.GreaterThan, "100"))
            .AddSorting(
                new Sorting("status", SortOrder.Ascending),
                new Sorting("total", SortOrder.Descending));

        var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(_orders.AsQueryable(), settings);

        var results = pagedQuery.ToList();

        // Status != Delivered AND Total > 100: Alice (150, Pending), Bob (200, Shipped), Eve (125, Shipped)
        results.Count.ShouldBe(3);
        countQuery.Count().ShouldBe(3);

        // Sorted by Status then Total DESC
        // Pending comes before Shipped alphabetically
        results[0].Status.ShouldBe("Pending");
        results[0].CustomerName.ShouldBe("Alice");
    }

    #endregion
}
