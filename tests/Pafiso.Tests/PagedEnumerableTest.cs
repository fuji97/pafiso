using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pafiso.Enumerables;
using Pafiso.Extensions;
using Shouldly;

namespace Pafiso.Tests;

public class PagedEnumerableTest {
    private List<TestEntity> _testData = null!;

    [SetUp]
    public void Setup() {
        _testData = Enumerable.Range(1, 100)
            .Select(i => new TestEntity { Id = i, Name = $"Entity {i}", Value = i * 10 })
            .ToList();
    }

    [Test]
    public void ToPagedList_ShouldReturnCorrectTotalEntriesAndPagedData() {
        var enumerable = _testData.AsEnumerable();
        var paging = Paging.FromPaging(0, 10);

        var pagedEnumerable = enumerable.WithSearchParameters(new SearchParameters(paging));
        var result = pagedEnumerable.ToPagedList();

        result.TotalEntries.ShouldBe(100);
        result.Entries.Count.ShouldBe(10);
        result.Entries[0].Id.ShouldBe(1);
        result.Entries[9].Id.ShouldBe(10);
    }

    [Test]
    public void ToPagedList_WithSecondPage_ShouldReturnCorrectData() {
        var enumerable = _testData.AsEnumerable();
        var paging = Paging.FromPaging(2, 10);

        var pagedEnumerable = enumerable.WithSearchParameters(new SearchParameters(paging));
        var result = pagedEnumerable.ToPagedList();

        result.TotalEntries.ShouldBe(100);
        result.Entries.Count.ShouldBe(10);
        result.Entries[0].Id.ShouldBe(21);
        result.Entries[9].Id.ShouldBe(30);
    }

    [Test]
    public void ToPagedList_WithFilter_ShouldReturnFilteredResults() {
        var enumerable = _testData.AsEnumerable();
        var paging = Paging.FromPaging(0, 5);
        var filter = new Filter("Value", FilterOperator.GreaterThan, "500");

        var pagedEnumerable = enumerable
            .Where(filter)
            .WithSearchParameters(new SearchParameters(paging));
        var result = pagedEnumerable.ToPagedList();

        result.TotalEntries.ShouldBe(50); // Entities 51-100 have Value > 500
        result.Entries.Count.ShouldBe(5);
        result.Entries[0].Value.ShouldBeGreaterThan(500);
    }

    [Test]
    public void ToPagedList_WithSorting_ShouldReturnSortedResults() {
        var enumerable = _testData.AsEnumerable();
        var paging = Paging.FromPaging(0, 10);
        var sorting = new Sorting("Value", SortOrder.Descending);

        var pagedEnumerable = enumerable
            .OrderBy(sorting)
            .WithSearchParameters(new SearchParameters(paging));
        var result = pagedEnumerable.ToPagedList();

        result.TotalEntries.ShouldBe(100);
        result.Entries.Count.ShouldBe(10);
        result.Entries[0].Value.ShouldBe(1000); // Highest value
        result.Entries[9].Value.ShouldBe(910);
    }

    [Test]
    public void ToPagedList_WithoutApplyQuery_ShouldReturnAllData() {
        var enumerable = _testData.AsEnumerable();

        var pagedEnumerable = enumerable.WithSearchParameters(new SearchParameters());
        var result = pagedEnumerable.ToPagedList();

        result.TotalEntries.ShouldBe(100);
        result.Entries.Count.ShouldBe(100);
    }

    [Test]
    public void PagedEnumerable_ShouldImplementIEnumerable() {
        var enumerable = _testData.AsEnumerable();
        var paging = Paging.FromPaging(0, 10);

        var pagedEnumerable = enumerable.WithSearchParameters(new SearchParameters(paging));

        var count = 0;
        foreach (var item in pagedEnumerable) {
            count++;
            item.ShouldNotBeNull();
        }

        count.ShouldBe(10);
    }

    [Test]
    public void PagedEnumerable_GetEnumerator_NonGeneric_ShouldWork() {
        var enumerable = _testData.AsEnumerable();
        var paging = Paging.FromPaging(0, 5);

        var pagedEnumerable = enumerable.WithSearchParameters(new SearchParameters(paging));

        IEnumerable nonGeneric = pagedEnumerable;
        var count = 0;
        foreach (var item in nonGeneric) {
            count++;
            item.ShouldNotBeNull();
        }

        count.ShouldBe(5);
    }

    [Test]
    public void ToPagedList_WithEmptyEnumerable_ShouldReturnEmptyResult() {
        var enumerable = Enumerable.Empty<TestEntity>();

        var pagedEnumerable = enumerable.WithSearchParameters(new SearchParameters());
        var result = pagedEnumerable.ToPagedList();

        result.TotalEntries.ShouldBe(0);
        result.Entries.Count.ShouldBe(0);
    }

    [Test]
    public void ToPagedList_WithPageBeyondData_ShouldReturnEmptyEntries() {
        var enumerable = _testData.AsEnumerable();
        var paging = Paging.FromPaging(20, 10); // Page 20 is beyond 100 items

        var pagedEnumerable = enumerable.WithSearchParameters(new SearchParameters(paging));
        var result = pagedEnumerable.ToPagedList();

        result.TotalEntries.ShouldBe(100);
        result.Entries.Count.ShouldBe(0);
    }

    private class TestEntity {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
