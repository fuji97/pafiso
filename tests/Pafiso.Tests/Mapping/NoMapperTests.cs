using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pafiso.Mapping;
using Shouldly;

namespace Pafiso.Tests.Mapping;

public class NoMapperTests {
    public class ProductSearchDto : MappingModel {
        public string? ProductName { get; set; }
    }

    public class Product {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    [Test]
    public void Filter_WithoutMapper_ThrowsOnApply() {
        // Create a SearchParameters with filters from dictionary using a mapper,
        // then verify that a filter without mapper (constructed with internal ctor) throws
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .Map(dto => dto.ProductName, entity => entity.Name);

        var filter = Filter.WithMapper("productName", FilterOperator.Contains, "test", mapper);

        var products = new List<Product> {
            new() { Id = 1, Name = "Test" }
        }.AsQueryable();

        // Filters with mapper should not throw
        Should.NotThrow(() => filter.ApplyFilter(products));
    }

    [Test]
    public void Sorting_WithoutMapper_ThrowsOnApply() {
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .Map(dto => dto.ProductName, entity => entity.Name);

        var sorting = Sorting.WithMapper("productName", SortOrder.Ascending, mapper);

        var products = new List<Product> {
            new() { Id = 1, Name = "Test" }
        }.AsQueryable();

        // Sortings with mapper should not throw
        Should.NotThrow(() => sorting.ApplyToIQueryable(products));
    }

    [Test]
    public void Filter_ToDictionary_ProducesValidOutput() {
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .Map(dto => dto.ProductName, entity => entity.Name);

        var filter = Filter.WithMapper("productName", FilterOperator.Contains, "test", mapper, true);

        var dict = filter.ToDictionary();

        dict["fields"].ShouldBe("productName");
        dict["op"].ShouldBe("contains");
        dict["val"].ShouldBe("test");
        dict["case"].ShouldBe("true");
    }

    [Test]
    public void Sorting_ToDictionary_ProducesValidOutput() {
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .Map(dto => dto.ProductName, entity => entity.Name);

        var sorting = Sorting.WithMapper("productName", SortOrder.Descending, mapper);

        var dict = sorting.ToDictionary();

        dict["prop"].ShouldBe("productName");
        dict["ord"].ShouldBe("desc");
    }

    [Test]
    public void Filter_Equality_Works() {
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .Map(dto => dto.ProductName, entity => entity.Name);

        var f1 = Filter.WithMapper("productName", FilterOperator.Contains, "test", mapper);
        var f2 = Filter.WithMapper("productName", FilterOperator.Contains, "test", mapper);
        var f3 = Filter.WithMapper("productName", FilterOperator.Equals, "test", mapper);

        (f1 == f2).ShouldBeTrue();
        (f1 != f3).ShouldBeTrue();
    }

    [Test]
    public void Sorting_Equality_Works() {
        var mapper = new FieldMapper<ProductSearchDto, Product>()
            .Map(dto => dto.ProductName, entity => entity.Name);

        var s1 = Sorting.WithMapper("productName", SortOrder.Ascending, mapper);
        var s2 = Sorting.WithMapper("productName", SortOrder.Ascending, mapper);
        var s3 = Sorting.WithMapper("productName", SortOrder.Descending, mapper);

        (s1 == s2).ShouldBeTrue();
        (s1 != s3).ShouldBeTrue();
    }
}
