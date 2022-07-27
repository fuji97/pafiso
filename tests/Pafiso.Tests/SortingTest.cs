using System.Collections.Generic;
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
    }

    private List<Foo> _foos = new();

    [SetUp]
    public void Setup() {
        _foos = new List<Foo> {
            new() { Id = 2, Name = "B" },
            new() { Id = 1, Name = "A" },
            new() { Id = 4, Name = "D" },
            new() { Id = 3, Name = "C" }
        };
    }

    [Test]
    public void CreateFromExpression() {
        var sort = Sorting.FromExpression<Foo>(x => x.Name, SortOrder.Ascending);
        var orderedList = _foos.OrderBy(sort).ToList();
        
        orderedList.Select(x => x.Name).Should()
            .BeEquivalentTo(_foos.Select(x => x.Name).OrderBy(x => x));
        // Assert.AreEqual(orderedList[0].Name, "A");
        // Assert.AreEqual(orderedList[1].Name, "B");
        // Assert.AreEqual(orderedList[2].Name, "C");
        // Assert.AreEqual(orderedList[3].Name, "D");
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
        // Assert.AreEqual(orderedList[0].Name, "A");
        // Assert.AreEqual(orderedList[1].Name, "B");
        // Assert.AreEqual(orderedList[2].Name, "C");
        // Assert.AreEqual(orderedList[3].Name, "D");
    }
}