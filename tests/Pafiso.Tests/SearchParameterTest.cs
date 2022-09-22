using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace Pafiso.Tests; 

public class SearchParameterTest {
    private SearchParameters _searchParameters = null!;
    
    [SetUp]
    public void Setup() {
        _searchParameters = new SearchParameters() {
            Filters = new List<Filter>() {
                new("Name", FilterOperator.Contains, "Franco", false),
                new("Age", FilterOperator.GreaterThan, 20.ToString(), true)
            },
            Paging = Paging.FromPaging(2, 10),
            Sortings = new List<Sorting>() {
                new Sorting("Name", SortOrder.Ascending),
                new Sorting("Age", SortOrder.Descending)
            }
        };
    }

    [Test]
    public void ToDictionary() {
        var dictionary = _searchParameters.ToDictionary();
        Assert.AreEqual(13, dictionary.Count);
        Assert.AreEqual("Age", dictionary["filters[1][fields]"]);
        Assert.AreEqual(SortOrder.Ascending.ToString(), dictionary["sortings[0][ord]"]);
        Assert.AreEqual("10", dictionary["take"]);
    }

    [Test]
    public void FromDictionary() {
        var dictionary = _searchParameters.ToDictionary();
        var searchParameters = SearchParameters.FromDictionary(dictionary);
        Assert.AreEqual(_searchParameters, searchParameters);
    }
    
    [Test]
    public void RemoveDuplicateSortings() {
        _searchParameters.Sortings.Add(new Sorting("Name", SortOrder.Descending));
        _searchParameters.Sortings.Add(new Sorting("Age", SortOrder.Ascending));

        var dictionary = _searchParameters.ToDictionary();
        var searchParameters = SearchParameters.FromDictionary(dictionary);
        searchParameters.Sortings.Should()
            .HaveCount(2)
            .And.ContainEquivalentOf(_searchParameters.Sortings[0])
            .And.ContainEquivalentOf(_searchParameters.Sortings[1]);
    }
}