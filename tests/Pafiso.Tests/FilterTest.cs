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
        public string? Description { get; set; }
        public SubFoo SubFoo { get; set; } = null!;
    }

    private record SubFoo(string SubName, int SubAge);

    private List<Foo> _foos = null!;
    private Filter _filter = null!;
    
    [SetUp]
    public void Setup() {
        _foos = [
            new Foo { Name = "John", Age = 30, Description = "This is a description", SubFoo = new SubFoo("Helo", 10) },
            new Foo { Name = "Jane", Age = 25, SubFoo = new SubFoo("Jj", 12) },
            new Foo {
                Name = "Joe", Age = 20, Description = "This is another description", SubFoo = new SubFoo("Jo", 14)
            },
            new Foo {
                Name = "Jack", Age = 15, Description = "And then, another description", SubFoo = new SubFoo("Jak", 15)
            },
            new Foo { Name = "Jill", Age = 10, SubFoo = new SubFoo("Jil", 16) }
        ];
        _filter = new Filter(nameof(Foo.Age), FilterOperator.GreaterThanOrEquals, "20");
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

    [Test]
    public void Equals() {
        var filter = Filter.FromExpression<Foo>(x => x.Age == 20);
        
        var filtered = _foos.Where(filter).ToList();

        filtered.Should().BeEquivalentTo(_foos.Where(x => x.Age == 20));
    }
    
    [Test]
    public void EqualsCases() {
        var filterInsensitive = new Filter(nameof(Foo.Name), FilterOperator.Equals, "john");
        var filterSensitive = new Filter(nameof(Foo.Name), FilterOperator.Equals, "john", true);
        
        var sensitiveFiltered = _foos.Where(filterSensitive).ToList();
        var insensitiveFiltered = _foos.Where(filterInsensitive).ToList();
        
        insensitiveFiltered.Count.Should().Be(1);
        insensitiveFiltered[0].Should().Be(_foos[0]);
        
        sensitiveFiltered.Count.Should().Be(0);
    }
    
    [Test]
    public void NotEquals() {
        var filter = Filter.FromExpression<Foo>(x => x.Age != 20);
        
        var filtered = _foos.Where(filter).ToList();

        filtered.Should().BeEquivalentTo(_foos.Where(x => x.Age != 20));
    }
    
    [Test]
    public void GreaterThan() {
        var filter = Filter.FromExpression<Foo>(x => x.Age > 20);
        
        var filtered = _foos.Where(filter).ToList();

        filtered.Should().BeEquivalentTo(_foos.Where(x => x.Age > 20));
    }

    [Test]
    public void LessThan() {
        var filter = Filter.FromExpression<Foo>(x => x.Age < 20);
        
        var filtered = _foos.Where(filter).ToList();

        filtered.Should().BeEquivalentTo(_foos.Where(x => x.Age < 20));
    }
    
    [Test]
    public void GreaterThanOrEquals() {
        var filter = Filter.FromExpression<Foo>(x => x.Age >= 20);
        
        var filtered = _foos.Where(filter).ToList();

        filtered.Should().BeEquivalentTo(_foos.Where(x => x.Age >= 20));
    }
    
    [Test]
    public void LessThanOrEquals() {
        var filter = Filter.FromExpression<Foo>(x => x.Age <= 20);
        
        var filtered = _foos.Where(filter).ToList();

        filtered.Should().BeEquivalentTo(_foos.Where(x => x.Age <= 20));
    }
    
    [Test]
    public void Contains() {
        var filter = Filter.FromExpression<Foo>(x => x.Name.Contains('o'));
        
        var filtered = _foos.Where(filter).ToList();

        filtered.Should().BeEquivalentTo(_foos.Where(x => x.Name.Contains('o')));
    }
    
    [Test]
    public void NotContains() {
        var filter = Filter.FromExpression<Foo>(x => !x.Name.Contains('o'));
        
        var filtered = _foos.Where(filter).ToList();

        filtered.Should().BeEquivalentTo(_foos.Where(x => !x.Name.Contains('o')));
    }

    [Test]
    public void IsNull() {
        var filter = Filter.FromExpression<Foo>(x => x.Description == null);
        
        var filtered = _foos.Where(filter).ToList();

        filtered.Should().BeEquivalentTo(_foos.Where(x => x.Description == null));
    }
    
    [Test]
    public void IsNotNull() {
        var filter = Filter.FromExpression<Foo>(x => x.Description != null);
        
        var filtered = _foos.Where(filter).ToList();

        filtered.Should().BeEquivalentTo(_foos.Where(x => x.Description != null));
    }

    [Test]
    public void NestedContains() {
        var filter = Filter.FromExpression<Foo>(x => x.SubFoo.SubName.Contains('o'));
        
        var filtered = _foos.Where(filter).ToList();
        
        filtered.Should().BeEquivalentTo(_foos.Where(x => x.SubFoo.SubName.Contains('o')));
    }

    [Test]
    public void MultipleFields() {
        var filter = Filter.FromExpression<Foo>(x => x.Name.Contains('o')).AddField(x => x.SubFoo.SubName);
        
        var filtered = _foos.Where(filter).ToList();

        filtered.Should().BeEquivalentTo(_foos.Where(x => x.Name.Contains('o') || x.SubFoo.SubName.Contains('o')));
    }

    [Test]
    public void ContainsCaseInsensitive() {
        var filter = Filter.FromExpression<Foo>(x => x.Name.Contains("JOHN"));
        
        var filtered = _foos.Where(filter).ToList();
        
        filtered.Should().BeEquivalentTo(_foos.Where(x => x.Name.ToLower().Contains("john")));
    }
}

