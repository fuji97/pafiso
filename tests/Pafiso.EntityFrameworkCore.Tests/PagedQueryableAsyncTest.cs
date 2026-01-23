using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Pafiso.EntityFrameworkCore.Enumerables;
using Pafiso.Extensions;
using Shouldly;

namespace Pafiso.EntityFrameworkCore.Tests;

public class PagedQueryableAsyncTest {
    private TestDbContext _context = null!;

    [SetUp]
    public void Setup() {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);

        // Seed test data
        var entities = Enumerable.Range(1, 100)
            .Select(i => new TestEntity { Id = i, Name = $"Entity {i}", Value = i * 10 })
            .ToList();

        _context.TestEntities.AddRange(entities);
        _context.SaveChanges();
    }

    [TearDown]
    public void TearDown() {
        _context.Dispose();
    }

    [Test]
    public async Task ToPagedListAsync_ShouldReturnCorrectTotalEntriesAndPagedData() {
        var queryable = _context.TestEntities.AsQueryable();
        var paging = Paging.FromPaging(0, 10);

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters(),
            query => query.Paging(paging));
        var result = await pagedQueryable.ToPagedListAsync();

        result.TotalEntries.ShouldBe(100);
        result.Entries.Count.ShouldBe(10);
        result.Entries[0].Id.ShouldBe(1);
        result.Entries[9].Id.ShouldBe(10);
    }

    [Test]
    public async Task ToPagedListAsync_WithSecondPage_ShouldReturnCorrectData() {
        var queryable = _context.TestEntities.AsQueryable();
        var paging = Paging.FromPaging(5, 8);

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters(),
            query => query.Paging(paging));
        var result = await pagedQueryable.ToPagedListAsync();

        result.TotalEntries.ShouldBe(100);
        result.Entries.Count.ShouldBe(8);
        result.Entries[0].Id.ShouldBe(41);
        result.Entries[7].Id.ShouldBe(48);
    }

    [Test]
    public async Task ToPagedListAsync_WithFilter_ShouldReturnFilteredResults() {
        var queryable = _context.TestEntities.AsQueryable();
        var paging = Paging.FromPaging(0, 10);
        var filter = new Filter("Value", FilterOperator.GreaterThan, "700");

        var pagedQueryable = queryable
            .Where(filter)
            .WithSearchParameters(new SearchParameters(),
                query => query.Paging(paging));
        var result = await pagedQueryable.ToPagedListAsync();

        result.TotalEntries.ShouldBe(30); // Entities 71-100 have Value > 700
        result.Entries.Count.ShouldBe(10);
        result.Entries.All(e => e.Value > 700).ShouldBeTrue();
    }

    [Test]
    public async Task ToPagedListAsync_WithSorting_ShouldReturnSortedResults() {
        var queryable = _context.TestEntities.AsQueryable();
        var paging = Paging.FromPaging(0, 10);
        var sorting = new Sorting("Value", SortOrder.Descending);

        var pagedQueryable = queryable
            .OrderBy(sorting)
            .WithSearchParameters(new SearchParameters(),
                query => query.Paging(paging));
        var result = await pagedQueryable.ToPagedListAsync();

        result.TotalEntries.ShouldBe(100);
        result.Entries.Count.ShouldBe(10);
        result.Entries[0].Value.ShouldBe(1000); // Highest value
        result.Entries[9].Value.ShouldBe(910);

        // Verify descending order
        for (int i = 0; i < result.Entries.Count - 1; i++) {
            result.Entries[i].Value.ShouldBeGreaterThan(result.Entries[i + 1].Value);
        }
    }

    [Test]
    public async Task ToPagedListAsync_WithComplexQuery_ShouldReturnCorrectResults() {
        var queryable = _context.TestEntities.AsQueryable();
        var paging = Paging.FromPaging(2, 5);
        var filter = new Filter("Value", FilterOperator.LessThan, "500");
        var sorting = new Sorting("Value", SortOrder.Descending);

        var pagedQueryable = queryable
            .Where(filter)
            .OrderBy(sorting)
            .WithSearchParameters(new SearchParameters(),
                query => query.Paging(paging));
        var result = await pagedQueryable.ToPagedListAsync();

        result.TotalEntries.ShouldBe(49); // Entities 1-49 have Value < 500
        result.Entries.Count.ShouldBe(5);
        result.Entries[0].Value.ShouldBe(390); // Third page (skip 10), descending
        result.Entries[4].Value.ShouldBe(350);
    }

    [Test]
    public async Task ToPagedListAsync_WithoutApplyQuery_ShouldReturnAllData() {
        var queryable = _context.TestEntities.AsQueryable();

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters());
        var result = await pagedQueryable.ToPagedListAsync();

        result.TotalEntries.ShouldBe(100);
        result.Entries.Count.ShouldBe(100);
    }

    [Test]
    public async Task ToPagedListAsync_WithEmptyQueryable_ShouldReturnEmptyResult() {
        var queryable = _context.TestEntities.Where(e => e.Id > 1000).AsQueryable();

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters());
        var result = await pagedQueryable.ToPagedListAsync();

        result.TotalEntries.ShouldBe(0);
        result.Entries.Count.ShouldBe(0);
    }

    [Test]
    public async Task ToPagedListAsync_WithPageBeyondData_ShouldReturnEmptyEntries() {
        var queryable = _context.TestEntities.AsQueryable();
        var paging = Paging.FromPaging(25, 10); // Page 25 is beyond 100 items

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters(),
            query => query.Paging(paging));
        var result = await pagedQueryable.ToPagedListAsync();

        result.TotalEntries.ShouldBe(100);
        result.Entries.Count.ShouldBe(0);
    }

    [Test]
    public async Task ToPagedListAsync_WithMultipleFilters_ShouldReturnCorrectResults() {
        var queryable = _context.TestEntities.AsQueryable();
        var paging = Paging.FromPaging(0, 10);
        var filter1 = new Filter("Value", FilterOperator.GreaterThanOrEquals, "300");
        var filter2 = new Filter("Value", FilterOperator.LessThanOrEquals, "600");

        var pagedQueryable = queryable
            .Where(filter1)
            .Where(filter2)
            .WithSearchParameters(new SearchParameters(),
                query => query.Paging(paging));
        var result = await pagedQueryable.ToPagedListAsync();

        result.TotalEntries.ShouldBe(31); // Entities 30-60 have 300 <= Value <= 600
        result.Entries.Count.ShouldBe(10);
        result.Entries.All(e => e.Value >= 300 && e.Value <= 600).ShouldBeTrue();
    }

    [Test]
    public async Task ToPagedListAsync_WithEFCoreIncludes_ShouldWork() {
        // Add related entities
        var parent = new ParentEntity { Id = 1, Name = "Parent 1" };
        parent.Children.Add(new ChildEntity { Id = 1, Name = "Child 1", ParentId = 1 });
        parent.Children.Add(new ChildEntity { Id = 2, Name = "Child 2", ParentId = 1 });
        _context.ParentEntities.Add(parent);
        _context.SaveChanges();

        var queryable = _context.ParentEntities.Include(p => p.Children).AsQueryable();
        var paging = Paging.FromPaging(0, 10);

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters(),
            query => query.Paging(paging));
        var result = await pagedQueryable.ToPagedListAsync();

        result.TotalEntries.ShouldBe(1);
        result.Entries.Count.ShouldBe(1);
        result.Entries[0].Children.Count.ShouldBe(2);
    }

    [Test]
    public async Task ToPagedListAsync_WithProjection_ShouldWork() {
        var queryable = _context.TestEntities
            .Select(e => new { e.Id, e.Name })
            .AsQueryable();
        var paging = Paging.FromPaging(0, 5);

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters(),
            query => query.Paging(paging));
        var result = await pagedQueryable.ToPagedListAsync();

        result.TotalEntries.ShouldBe(100);
        result.Entries.Count.ShouldBe(5);
        result.Entries[0].Id.ShouldBe(1);
        result.Entries[0].Name.ShouldBe("Entity 1");
    }

    [Test]
    public void PagedQueryable_ShouldImplementIQueryable() {
        var queryable = _context.TestEntities.AsQueryable();
        var paging = Paging.FromPaging(0, 10);

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters(),
            query => query.Paging(paging));

        pagedQueryable.ShouldBeAssignableTo<IQueryable<TestEntity>>();
        pagedQueryable.ElementType.ShouldBe(typeof(TestEntity));
        pagedQueryable.Provider.ShouldNotBeNull();
        pagedQueryable.Expression.ShouldNotBeNull();
    }

    [Test]
    public async Task ToPagedListAsync_WithPartialPage_ShouldReturnRemainingItems() {
        var queryable = _context.TestEntities.AsQueryable();
        var paging = Paging.FromPaging(9, 10); // Last page with 10 items per page

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters(),
            query => query.Paging(paging));
        var result = await pagedQueryable.ToPagedListAsync();

        result.TotalEntries.ShouldBe(100);
        result.Entries.Count.ShouldBe(10); // Items 91-100
        result.Entries[0].Id.ShouldBe(91);
        result.Entries[9].Id.ShouldBe(100);
    }

    [Test]
    public async Task ToPagedListAsync_MultipleCallsSameInstance_ShouldProduceSameResults() {
        var queryable = _context.TestEntities.AsQueryable();
        var paging = Paging.FromPaging(1, 10);

        var pagedQueryable = queryable.WithSearchParameters(new SearchParameters(),
            query => query.Paging(paging));

        var result1 = await pagedQueryable.ToPagedListAsync();
        var result2 = await pagedQueryable.ToPagedListAsync();

        result1.TotalEntries.ShouldBe(result2.TotalEntries);
        result1.Entries.Count.ShouldBe(result2.Entries.Count);
        result1.Entries[0].Id.ShouldBe(result2.Entries[0].Id);
    }

    private class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options) {
        public DbSet<TestEntity> TestEntities { get; set; } = null!;
        public DbSet<ParentEntity> ParentEntities { get; set; } = null!;
        public DbSet<ChildEntity> ChildEntities { get; set; } = null!;
    }

    private class TestEntity {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    private class ParentEntity {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<ChildEntity> Children { get; set; } = new();
    }

    private class ChildEntity {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ParentId { get; set; }
        public ParentEntity Parent { get; set; } = null!;
    }
}
