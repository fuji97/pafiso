using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;

namespace Pafiso.Tests;

public class SearchParameterTest {
    private SearchParameters _searchParameters = null!;

    [SetUp]
    public void Setup() {
        _searchParameters = new SearchParameters() {
            Filters = [
                new Filter("Name", FilterOperator.Contains, "Franco", false),
                new Filter("Age", FilterOperator.GreaterThan, 20.ToString(), true)
            ],
            Paging = Paging.FromPaging(2, 10),
            Sortings = [
                new Sorting("Name", SortOrder.Ascending),
                new Sorting("Age", SortOrder.Descending)
            ]
        };
    }

    [Test]
    public void ToDictionary() {
        var dictionary = _searchParameters.ToDictionary();
        dictionary.Count.ShouldBe(13);
        dictionary["filters[1][fields]"].ShouldBe("Age");
        dictionary["sortings[0][ord]"].ShouldBe("asc");
        dictionary["take"].ShouldBe("10");
    }

    [Test]
    public void FromDictionary() {
        var dictionary = _searchParameters.ToDictionary();
        var searchParameters = SearchParameters.FromDictionary(dictionary);
        searchParameters.Filters.ShouldBe(_searchParameters.Filters);
        searchParameters.Paging.ShouldBe(_searchParameters.Paging);
        searchParameters.Sortings.ShouldBe(_searchParameters.Sortings);
    }

    [Test]
    public void RemoveDuplicateSortings() {
        _searchParameters.Sortings.Add(new Sorting("Name", SortOrder.Descending));
        _searchParameters.Sortings.Add(new Sorting("Age", SortOrder.Ascending));

        var dictionary = _searchParameters.ToDictionary();
        var searchParameters = SearchParameters.FromDictionary(dictionary);
        searchParameters.Sortings.Count.ShouldBe(2);
        searchParameters.Sortings.ShouldContain(_searchParameters.Sortings[0]);
        searchParameters.Sortings.ShouldContain(_searchParameters.Sortings[1]);
    }
}
