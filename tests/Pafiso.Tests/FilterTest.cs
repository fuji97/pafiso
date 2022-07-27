using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Pafiso.Util;

namespace Pafiso.Tests; 

public class FilterTest {
    private class Foo {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
    }

    private List<Foo> _foos = null!;
    private Filter _filter = null!;
    
    [SetUp]
    public void Setup() {
        _foos = new() {
            new Foo { Name = "John", Age = 30 },
            new Foo { Name = "Jane", Age = 25 },
            new Foo { Name = "Joe", Age = 20 },
            new Foo { Name = "Jack", Age = 15 },
            new Foo { Name = "Jill", Age = 10 },
        };
        _filter = new Filter(nameof(Foo.Age), FilterOperator.GreaterThanOrEquals, "20");
    }

    [Test]
    public void Equal() {
        var filterInsensitive = new Filter(nameof(Foo.Name), FilterOperator.Equals, "john");
        var filterSensitive = new Filter(nameof(Foo.Name), FilterOperator.Equals, "john", true);
        
        var sensitiveFiltered = _foos.Where(filterSensitive).ToList();
        var insensitiveFiltered = _foos.Where(filterInsensitive).ToList();
        
        Assert.AreEqual(1, insensitiveFiltered.Count);
        Assert.AreEqual(_foos[0], insensitiveFiltered[0]);
        
        Assert.AreEqual(0, sensitiveFiltered.Count);
    }

    [Test]
    public void SerializeAndDeserialize() {
        var query = _filter.ToDictionary();
        var filter = Filter.FromDictionary(query);

        filter.Fields.Should().BeEquivalentTo(_filter.Fields);
        filter.Operator.Should().Be(_filter.Operator);
        filter.Value.Should().Be(_filter.Value);
        filter.CaseSensitive.Should().Be(_filter.CaseSensitive);
    }
}