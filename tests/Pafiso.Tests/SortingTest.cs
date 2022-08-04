﻿using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using Pafiso.Util;

namespace Pafiso.Tests; 

public class SortingTest {
    public class Foo {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public Bar Bar { get; set; } = null!;
    }

    public class Bar {
        public int Id { get; set; }
        public Bar(int id) {
            Id = id;
        }
    }

    private List<Foo> _foos = new();

    [SetUp]
    public void Setup() {
        _foos = new List<Foo> {
            new() { Id = 2, Name = "B", Bar = new Bar(5) },
            new() { Id = 1, Name = "A", Bar = new Bar(8) },
            new() { Id = 4, Name = "D", Bar = new Bar(2) },
            new() { Id = 3, Name = "C", Bar = new Bar(3) }
        };
    }

    [Test]
    public void CreateFromExpression() {
        var sort = Sorting.FromExpression<Foo>(x => x.Name, SortOrder.Ascending);
        var orderedList = _foos.OrderBy(sort).ToList();
        
        orderedList.Select(x => x.Name).Should()
            .BeEquivalentTo(_foos.Select(x => x.Name).OrderBy(x => x));
    }

    [Test]
    public void CreateFromTypedExpression() {
        var sort = Sorting<Foo>.FromExpression(x => x.Name, SortOrder.Ascending);
        
        var orderedList = _foos.OrderBy(sort).ToList();
        
        orderedList.Select(x => x.Name).Should()
            .BeEquivalentTo(_foos.Select(x => x.Name).OrderBy(x => x));
    }

    [Test]
    public void SortByNestedProperties() {
        var sort = Sorting<Foo>.FromExpression(x => x.Bar.Id, SortOrder.Ascending);
        
        var orderedList = _foos.OrderBy(sort).ToList();
        
        orderedList.Should()
            .BeEquivalentTo(_foos.OrderBy(x => x.Bar.Id));
    }

    [Test]
    public void SerializeAndDeserialize() {
        var sort = Sorting.FromExpression<Foo>(x => x.Name, SortOrder.Ascending);

        var serialized = JsonSerializer.Serialize(sort);
        var deserializedSort = JsonSerializer.Deserialize<Sorting>(serialized);

        deserializedSort.Should().NotBeNull();
        var orderedList = _foos.OrderBy(deserializedSort!).ToList();
        
        orderedList.Select(x => x.Name).Should()
            .BeEquivalentTo(_foos.Select(x => x.Name).OrderBy(x => x));
    }
}