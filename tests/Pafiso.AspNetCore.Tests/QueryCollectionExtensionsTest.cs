using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using Pafiso.AspNetCore;
using Shouldly;

namespace Pafiso.AspNetCore.Tests;

public class QueryCollectionExtensionsTest {
    [Test]
    public void ToSearchParameters_WithFilters() {
        var query = new QueryCollection(new Dictionary<string, StringValues> {
            { "filters[0][fields]", "Name" },
            { "filters[0][op]", "contains" },
            { "filters[0][val]", "phone" },
            { "filters[0][case]", "true" }
        });

        var searchParameters = query.ToSearchParameters();

        searchParameters.Filters.Count.ShouldBe(1);
        searchParameters.Filters[0].Fields.ShouldBe(["Name"]);
        searchParameters.Filters[0].Operator.ShouldBe(FilterOperator.Contains);
        searchParameters.Filters[0].Value.ShouldBe("phone");
        searchParameters.Filters[0].CaseSensitive.ShouldBeTrue();
    }

    [Test]
    public void ToSearchParameters_WithSortings() {
        var query = new QueryCollection(new Dictionary<string, StringValues> {
            { "sortings[0][prop]", "Price" },
            { "sortings[0][ord]", "desc" },
            { "sortings[1][prop]", "Name" },
            { "sortings[1][ord]", "asc" }
        });

        var searchParameters = query.ToSearchParameters();

        searchParameters.Sortings.Count.ShouldBe(2);
        searchParameters.Sortings[0].PropertyName.ShouldBe("Price");
        searchParameters.Sortings[0].SortOrder.ShouldBe(SortOrder.Descending);
        searchParameters.Sortings[1].PropertyName.ShouldBe("Name");
        searchParameters.Sortings[1].SortOrder.ShouldBe(SortOrder.Ascending);
    }

    [Test]
    public void ToSearchParameters_WithPaging() {
        var query = new QueryCollection(new Dictionary<string, StringValues> {
            { "skip", "10" },
            { "take", "20" }
        });

        var searchParameters = query.ToSearchParameters();

        searchParameters.Paging.ShouldNotBeNull();
        searchParameters.Paging!.Skip.ShouldBe(10);
        searchParameters.Paging.Take.ShouldBe(20);
    }

    [Test]
    public void ToSearchParameters_WithAllParameters() {
        var query = new QueryCollection(new Dictionary<string, StringValues> {
            { "filters[0][fields]", "Name" },
            { "filters[0][op]", "eq" },
            { "filters[0][val]", "Test" },
            { "filters[0][case]", "false" },
            { "sortings[0][prop]", "CreatedAt" },
            { "sortings[0][ord]", "desc" },
            { "skip", "0" },
            { "take", "10" }
        });

        var searchParameters = query.ToSearchParameters();

        searchParameters.Filters.Count.ShouldBe(1);
        searchParameters.Sortings.Count.ShouldBe(1);
        searchParameters.Paging.ShouldNotBeNull();
    }

    [Test]
    public void ToSearchParameters_EmptyQuery() {
        var query = new QueryCollection(new Dictionary<string, StringValues>());

        var searchParameters = query.ToSearchParameters();

        searchParameters.Filters.ShouldBeEmpty();
        searchParameters.Sortings.ShouldBeEmpty();
        searchParameters.Paging.ShouldBeNull();
    }

    [Test]
    public void ToSearchParameters_MultipleFieldsFilter() {
        var query = new QueryCollection(new Dictionary<string, StringValues> {
            { "filters[0][fields]", "Name,Description" },
            { "filters[0][op]", "contains" },
            { "filters[0][val]", "search" },
            { "filters[0][case]", "false" }
        });

        var searchParameters = query.ToSearchParameters();

        searchParameters.Filters.Count.ShouldBe(1);
        searchParameters.Filters[0].Fields.ShouldBe(["Name", "Description"]);
    }
}
