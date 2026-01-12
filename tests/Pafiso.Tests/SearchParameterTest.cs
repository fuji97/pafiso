using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

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
        dictionary.Count.Should().Be(13);
        dictionary["filters[1][fields]"].Should().Be("Age");
        dictionary["sortings[0][ord]"].Should().Be(nameof(SortOrder.Ascending));
        dictionary["take"].Should().Be("10");
    }

    [Test]
    public void FromDictionary() {
        var dictionary = _searchParameters.ToDictionary();
        var searchParameters = SearchParameters.FromDictionary(dictionary);
        searchParameters.Should().BeEquivalentTo(_searchParameters);
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

