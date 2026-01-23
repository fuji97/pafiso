using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using Pafiso.Enumerables;
using Pafiso.Extensions;
using Shouldly;

namespace Pafiso.Tests;

public class PagedQueryableTest {
    private List<TestEntity> _testData = null!;

    [SetUp]
    public void Setup() {
        _testData = Enumerable.Range(1, 100)
            .Select(i => new TestEntity { Id = i, Name = $"Entity {i}", Value = i * 10 })
            .ToList();
    }

    [Test]
    public void ToPagedList_ShouldReturnCorrectTotalEntriesAndPagedData() {
        var queryable = _testData.AsQueryable();
        var paging = Paging.FromPaging(0, 10);

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters(),
            query => query.Paging(paging));
        var result = pagedQueryable.ToPagedList();

        result.TotalEntries.ShouldBe(100);
        result.Entries.Count.ShouldBe(10);
        result.Entries[0].Id.ShouldBe(1);
        result.Entries[9].Id.ShouldBe(10);
    }

    [Test]
    public void ToPagedList_WithSecondPage_ShouldReturnCorrectData() {
        var queryable = _testData.AsQueryable();
        var paging = Paging.FromPaging(3, 15);

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters(),
            query => query.Paging(paging));
        var result = pagedQueryable.ToPagedList();

        result.TotalEntries.ShouldBe(100);
        result.Entries.Count.ShouldBe(15);
        result.Entries[0].Id.ShouldBe(46);
        result.Entries[14].Id.ShouldBe(60);
    }

    [Test]
    public void ToPagedList_WithFilter_ShouldReturnFilteredResults() {
        var queryable = _testData.AsQueryable();
        var paging = Paging.FromPaging(0, 10);
        var filter = new Filter("Value", FilterOperator.LessThanOrEquals, "300");

        var pagedQueryable = queryable
            .Where(filter)
            .WithSearchParameters(new SearchParameters(),
                query => query.Paging(paging));
        var result = pagedQueryable.ToPagedList();

        result.TotalEntries.ShouldBe(30); // Entities 1-30 have Value <= 300
        result.Entries.Count.ShouldBe(10);
        result.Entries.All(e => e.Value <= 300).ShouldBeTrue();
    }

    [Test]
    public void ToPagedList_WithSorting_ShouldReturnSortedResults() {
        var queryable = _testData.AsQueryable();
        var paging = Paging.FromPaging(0, 10);
        var sorting = new Sorting("Name", SortOrder.Descending);

        var pagedQueryable = queryable
            .OrderBy(sorting)
            .WithSearchParameters(new SearchParameters(),
                query => query.Paging(paging));
        var result = pagedQueryable.ToPagedList();

        result.TotalEntries.ShouldBe(100);
        result.Entries.Count.ShouldBe(10);
        // Should be sorted by Name descending: "Entity 99", "Entity 98", etc.
        // But "Entity 9" comes before "Entity 99" in string sort
        result.Entries[0].Name.ShouldBe("Entity 99");
    }

    [Test]
    public void ToPagedList_WithComplexQuery_ShouldReturnCorrectResults() {
        var queryable = _testData.AsQueryable();
        var paging = Paging.FromPaging(1, 5);
        var filter = new Filter("Value", FilterOperator.GreaterThanOrEquals, "200");
        var sorting = new Sorting("Value", SortOrder.Ascending);

        var pagedQueryable = queryable
            .Where(filter)
            .OrderBy(sorting)
            .WithSearchParameters(new SearchParameters(),
                query => query.Paging(paging));
        var result = pagedQueryable.ToPagedList();

        result.TotalEntries.ShouldBe(81); // Entities 20-100 have Value >= 200
        result.Entries.Count.ShouldBe(5);
        result.Entries[0].Value.ShouldBe(250); // Second page, so skip first 5
        result.Entries[4].Value.ShouldBe(290);
    }

    [Test]
    public void ToPagedList_WithoutApplyQuery_ShouldReturnAllData() {
        var queryable = _testData.AsQueryable();

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters());
        var result = pagedQueryable.ToPagedList();

        result.TotalEntries.ShouldBe(100);
        result.Entries.Count.ShouldBe(100);
    }

    [Test]
    public void PagedQueryable_ShouldImplementIQueryable() {
        var queryable = _testData.AsQueryable();
        var paging = Paging.FromPaging(0, 10);

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters(),
            query => query.Paging(paging));

        pagedQueryable.ShouldBeAssignableTo<IQueryable<TestEntity>>();
        pagedQueryable.ElementType.ShouldBe(typeof(TestEntity));
        pagedQueryable.Provider.ShouldNotBeNull();
        pagedQueryable.Expression.ShouldNotBeNull();
    }

    [Test]
    public void PagedQueryable_GetEnumerator_ShouldWork() {
        var queryable = _testData.AsQueryable();
        var paging = Paging.FromPaging(0, 10);

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters(),
            query => query.Paging(paging));

        var count = 0;
        foreach (var item in pagedQueryable) {
            count++;
            item.ShouldNotBeNull();
        }

        count.ShouldBe(10);
    }

    [Test]
    public void PagedQueryable_GetEnumerator_NonGeneric_ShouldWork() {
        var queryable = _testData.AsQueryable();
        var paging = Paging.FromPaging(0, 5);

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters(),
            query => query.Paging(paging));

        IEnumerable nonGeneric = pagedQueryable;
        var count = 0;
        foreach (var item in nonGeneric) {
            count++;
            item.ShouldNotBeNull();
        }

        count.ShouldBe(5);
    }

    [Test]
    public void ToPagedList_WithEmptyQueryable_ShouldReturnEmptyResult() {
        var queryable = Enumerable.Empty<TestEntity>().AsQueryable();

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters());
        var result = pagedQueryable.ToPagedList();

        result.TotalEntries.ShouldBe(0);
        result.Entries.Count.ShouldBe(0);
    }

    [Test]
    public void ToPagedList_WithPageBeyondData_ShouldReturnEmptyEntries() {
        var queryable = _testData.AsQueryable();
        var paging = Paging.FromPaging(15, 10); // Page 15 is beyond 100 items

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters(),
            query => query.Paging(paging));
        var result = pagedQueryable.ToPagedList();

        result.TotalEntries.ShouldBe(100);
        result.Entries.Count.ShouldBe(0);
    }

    [Test]
    public void PagedQueryable_CanBeUsedWithLinqMethods() {
        var queryable = _testData.AsQueryable();
        var paging = Paging.FromPaging(0, 20);

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters(),
            query => query.Paging(paging));

        // Should be able to apply additional LINQ operations
        var count = pagedQueryable.Count();
        var first = pagedQueryable.First();
        var any = pagedQueryable.Any(e => e.Id > 5);

        count.ShouldBe(20);
        first.Id.ShouldBe(1);
        any.ShouldBeTrue();
    }

    [Test]
    public void PagedQueryable_Properties_ShouldReflectEntriesQuery() {
        var queryable = _testData.AsQueryable();
        var paging = Paging.FromPaging(0, 10);

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters(),
            query => query.Where(e => e.Value > 100).Paging(paging));

        pagedQueryable.ElementType.ShouldBe(typeof(TestEntity));
        pagedQueryable.Expression.ShouldNotBeNull();
        pagedQueryable.Provider.ShouldNotBeNull();
    }

    private class TestEntity {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
