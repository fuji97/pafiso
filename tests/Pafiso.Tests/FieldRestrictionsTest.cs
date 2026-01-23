using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace Pafiso.Tests;

public class FieldRestrictionsTest {
    private class Product {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public string Secret { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public SubProduct Sub { get; set; } = null!;
    }

    private record SubProduct(string SubName);

    private List<Product> _products = null!;

    [SetUp]
    public void Setup() {
        _products = [
            new Product { Id = 1, Name = "Alpha", Price = 30, Secret = "X", CreatedAt = DateTime.Now, Sub = new SubProduct("SubAlpha") },
            new Product { Id = 2, Name = "Beta", Price = 20, Secret = "Y", CreatedAt = DateTime.Now.AddDays(-1), Sub = new SubProduct("SubBeta") },
            new Product { Id = 3, Name = "Gamma", Price = 10, Secret = "Z", CreatedAt = DateTime.Now.AddDays(-2), Sub = new SubProduct("SubGamma") }
        ];
    }

    #region Filter Blocklist Tests

    [Test]
    public void BlockFiltering_BlocksSpecifiedField() {
        var searchParams = new SearchParameters()
            .AddFilters(
                new Filter(nameof(Product.Name), FilterOperator.Equals, "Alpha"),
                new Filter(nameof(Product.Secret), FilterOperator.Equals, "X")
            );

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions.BlockFiltering(nameof(Product.Secret))
        );

        var results = pagedQuery.ToList();
        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("Alpha");
    }

    [Test]
    public void BlockFiltering_ExpressionBased_BlocksSpecifiedField() {
        var searchParams = new SearchParameters()
            .AddFilters(
                new Filter(nameof(Product.Name), FilterOperator.Equals, "Alpha"),
                new Filter(nameof(Product.Secret), FilterOperator.Equals, "X")
            );

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions.BlockFiltering<Product>(x => x.Secret)
        );

        var results = pagedQuery.ToList();
        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("Alpha");
    }

    [Test]
    public void BlockFiltering_AllowsNonBlockedFields() {
        var searchParams = new SearchParameters()
            .AddFilters(new Filter(nameof(Product.Price), FilterOperator.GreaterThan, "15"));

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions.BlockFiltering(nameof(Product.Secret))
        );

        var results = pagedQuery.ToList();
        results.Count.ShouldBe(2);
    }

    #endregion

    #region Filter Allowlist Tests

    [Test]
    public void AllowFiltering_OnlyAllowsSpecifiedFields() {
        var searchParams = new SearchParameters()
            .AddFilters(
                new Filter(nameof(Product.Name), FilterOperator.Equals, "Alpha"),
                new Filter(nameof(Product.Secret), FilterOperator.Equals, "X")
            );

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions.AllowFiltering(nameof(Product.Name), nameof(Product.Price))
        );

        // Secret filter should be ignored, only Name filter applies
        var results = pagedQuery.ToList();
        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("Alpha");
    }

    [Test]
    public void AllowFiltering_ExpressionBased_OnlyAllowsSpecifiedFields() {
        var searchParams = new SearchParameters()
            .AddFilters(
                new Filter(nameof(Product.Name), FilterOperator.Contains, "a"),
                new Filter(nameof(Product.Secret), FilterOperator.Equals, "X")
            );

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions.AllowFiltering<Product>(x => x.Name, x => x.Price)
        );

        // Secret filter ignored; Name contains 'a' matches Alpha, Beta, Gamma (case insensitive)
        var results = pagedQuery.ToList();
        results.Count.ShouldBe(3);
    }

    [Test]
    public void AllowFiltering_BlocksUnspecifiedFields() {
        var searchParams = new SearchParameters()
            .AddFilters(new Filter(nameof(Product.Secret), FilterOperator.Equals, "X"));

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions.AllowFiltering(nameof(Product.Name))
        );

        // Secret is not allowed, so filter is ignored - all products returned
        var results = pagedQuery.ToList();
        results.Count.ShouldBe(3);
    }

    #endregion

    #region Sorting Blocklist Tests

    [Test]
    public void BlockSorting_BlocksSpecifiedField() {
        var searchParams = new SearchParameters()
            .AddSorting(
                new Sorting(nameof(Product.Secret), SortOrder.Ascending),
                new Sorting(nameof(Product.Price), SortOrder.Descending)
            );

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions.BlockSorting(nameof(Product.Secret))
        );

        // Secret sorting ignored, Price descending applied
        var results = pagedQuery.ToList();
        results.Select(x => x.Price).ShouldBe([30m, 20m, 10m]);
    }

    [Test]
    public void BlockSorting_ExpressionBased_BlocksSpecifiedField() {
        var searchParams = new SearchParameters()
            .AddSorting(new Sorting(nameof(Product.Secret), SortOrder.Ascending));

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions.BlockSorting<Product>(x => x.Secret)
        );

        // All sorting blocked - original order preserved
        var results = pagedQuery.ToList();
        results.Select(x => x.Id).ShouldBe([1, 2, 3]);
    }

    #endregion

    #region Sorting Allowlist Tests

    [Test]
    public void AllowSorting_OnlyAllowsSpecifiedFields() {
        var searchParams = new SearchParameters()
            .AddSorting(
                new Sorting(nameof(Product.Secret), SortOrder.Ascending),
                new Sorting(nameof(Product.Price), SortOrder.Ascending)
            );

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions.AllowSorting(nameof(Product.Name), nameof(Product.Price))
        );

        // Secret not allowed; Price ascending applied
        var results = pagedQuery.ToList();
        results.Select(x => x.Price).ShouldBe([10m, 20m, 30m]);
    }

    [Test]
    public void AllowSorting_ExpressionBased_OnlyAllowsSpecifiedFields() {
        var searchParams = new SearchParameters()
            .AddSorting(new Sorting(nameof(Product.Name), SortOrder.Descending));

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions.AllowSorting<Product>(x => x.Name, x => x.CreatedAt)
        );

        // Name is allowed, descending order
        var results = pagedQuery.ToList();
        results.Select(x => x.Name).ShouldBe(["Gamma", "Beta", "Alpha"]);
    }

    #endregion

    #region Mixed Restrictions Tests

    [Test]
    public void MixedRestrictions_FilterAllowlistAndSortingBlocklist() {
        var searchParams = new SearchParameters()
            .AddFilters(
                new Filter(nameof(Product.Name), FilterOperator.Contains, "a"),
                new Filter(nameof(Product.Secret), FilterOperator.Equals, "X")
            )
            .AddSorting(
                new Sorting(nameof(Product.Secret), SortOrder.Ascending),
                new Sorting(nameof(Product.Price), SortOrder.Ascending)
            );

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions
                .AllowFiltering(nameof(Product.Name), nameof(Product.Price))
                .BlockSorting(nameof(Product.Secret))
        );

        // Name filter applied (contains 'a'), Secret filter ignored
        // Secret sorting blocked, Price sorting applied
        var results = pagedQuery.ToList();
        results.Count.ShouldBe(3);
        results.Select(x => x.Price).ShouldBe([10m, 20m, 30m]);
    }

    [Test]
    public void MixedRestrictions_SeparateFilterAndSortRestrictions() {
        var searchParams = new SearchParameters()
            .AddFilters(new Filter(nameof(Product.Price), FilterOperator.GreaterThan, "15"))
            .AddSorting(new Sorting(nameof(Product.Name), SortOrder.Ascending));

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions
                .AllowFiltering(nameof(Product.Price))
                .AllowSorting(nameof(Product.Name))
        );

        // Price > 15 filter, Name sorting
        var results = pagedQuery.ToList();
        results.Count.ShouldBe(2);
        results.Select(x => x.Name).ShouldBe(["Alpha", "Beta"]);
    }

    #endregion

    #region Edge Cases

    [Test]
    public void NoRestrictions_BehavesLikeOriginalMethod() {
        var searchParams = new SearchParameters()
            .AddFilters(new Filter(nameof(Product.Price), FilterOperator.GreaterThan, "15"))
            .AddSorting(new Sorting(nameof(Product.Name), SortOrder.Ascending));

        var (_, pagedQueryWithRestrictions) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => { } // No restrictions configured
        );

        var (_, pagedQueryWithoutRestrictions) = searchParams.ApplyToIQueryable(_products.AsQueryable());

        pagedQueryWithRestrictions.ToList().ShouldBe(pagedQueryWithoutRestrictions.ToList());
    }

    [Test]
    public void AllFiltersBlocked_ReturnsAllResults() {
        var searchParams = new SearchParameters()
            .AddFilters(new Filter(nameof(Product.Secret), FilterOperator.Equals, "X"));

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions.BlockFiltering(nameof(Product.Secret))
        );

        var results = pagedQuery.ToList();
        results.Count.ShouldBe(3);
    }

    [Test]
    public void AllSortingsBlocked_PreservesOriginalOrder() {
        var searchParams = new SearchParameters()
            .AddSorting(new Sorting(nameof(Product.Price), SortOrder.Descending));

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions.BlockSorting(nameof(Product.Price))
        );

        var results = pagedQuery.ToList();
        results.Select(x => x.Id).ShouldBe([1, 2, 3]);
    }

    [Test]
    public void NestedProperty_StringBased() {
        var searchParams = new SearchParameters()
            .AddFilters(new Filter("Sub.SubName", FilterOperator.Contains, "Alpha"));

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions.AllowFiltering("Sub.SubName")
        );

        var results = pagedQuery.ToList();
        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("Alpha");
    }

    [Test]
    public void NestedProperty_ExpressionBased() {
        var searchParams = new SearchParameters()
            .AddFilters(new Filter("Sub.SubName", FilterOperator.Contains, "Beta"));

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions.AllowFiltering<Product>(x => x.Sub.SubName)
        );

        var results = pagedQuery.ToList();
        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("Beta");
    }

    [Test]
    public void BlocklistTakesPrecedenceOverAllowlist_ForFiltering() {
        var searchParams = new SearchParameters()
            .AddFilters(new Filter(nameof(Product.Name), FilterOperator.Equals, "Alpha"));

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions
                .AllowFiltering(nameof(Product.Name))
                .BlockFiltering(nameof(Product.Name))
        );

        // Name is both allowed and blocked - block takes precedence
        var results = pagedQuery.ToList();
        results.Count.ShouldBe(3);
    }

    [Test]
    public void BlocklistTakesPrecedenceOverAllowlist_ForSorting() {
        var searchParams = new SearchParameters()
            .AddSorting(new Sorting(nameof(Product.Name), SortOrder.Ascending));

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions
                .AllowSorting(nameof(Product.Name))
                .BlockSorting(nameof(Product.Name))
        );

        // Name is both allowed and blocked - block takes precedence, original order preserved
        var results = pagedQuery.ToList();
        results.Select(x => x.Id).ShouldBe([1, 2, 3]);
    }

    [Test]
    public void MultiFieldFilter_PartialRestriction_OnlyAllowedFieldsApplied() {
        // Create a multi-field filter: Name OR Secret contains 'a'
        // Alpha has Name containing 'a' and Secret = 'X'
        // Beta has Name containing 'a' and Secret = 'Y'
        // Gamma has Name containing 'a' and Secret = 'Z'
        var filter = new Filter([nameof(Product.Name), nameof(Product.Secret)], FilterOperator.Contains, "a");
        var searchParams = new SearchParameters().AddFilters(filter);

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions.BlockFiltering(nameof(Product.Secret))
        );

        // Secret field is blocked, so only Name field should be used in the filter
        // All products have 'a' in Name (Alpha, Beta, Gamma - case insensitive)
        var results = pagedQuery.ToList();
        results.Count.ShouldBe(3);
    }

    [Test]
    public void MultiFieldFilter_AllFieldsBlocked_FilterSkipped() {
        var filter = new Filter([nameof(Product.Secret), "InternalField"], FilterOperator.Contains, "test");
        var searchParams = new SearchParameters().AddFilters(filter);

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions.BlockFiltering(nameof(Product.Secret), "InternalField")
        );

        // All fields in the filter are blocked, so filter is skipped entirely
        var results = pagedQuery.ToList();
        results.Count.ShouldBe(3);
    }

    [Test]
    public void MultiFieldFilter_WithAllowlist_OnlyAllowedFieldsApplied() {
        // Create a filter: Name OR Secret = 'Alpha' (looking for the value)
        var filter = new Filter([nameof(Product.Name), nameof(Product.Secret)], FilterOperator.Equals, "Alpha");
        var searchParams = new SearchParameters().AddFilters(filter);

        var (_, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions.AllowFiltering(nameof(Product.Name))
        );

        // Only Name is allowed; filter becomes Name = 'Alpha'
        var results = pagedQuery.ToList();
        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("Alpha");
    }

    [Test]
    public void PagingStillApplied_WithRestrictions() {
        var searchParams = new SearchParameters {
            Paging = Paging.FromSkipTake(1, 1)
        };
        searchParams.AddSorting(new Sorting(nameof(Product.Name), SortOrder.Ascending));

        var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(
            _products.AsQueryable(),
            restrictions => restrictions.AllowSorting(nameof(Product.Name))
        );

        countQuery.Count().ShouldBe(3);
        var results = pagedQuery.ToList();
        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("Beta"); // Second item after Alpha
    }

    #endregion
}
