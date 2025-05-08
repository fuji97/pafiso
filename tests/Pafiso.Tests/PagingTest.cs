using System.Linq;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using Pafiso.Util;

namespace Pafiso.Tests; 

public class PagingTest {
    [Test]
    public void FromPaging() {
        var paging = Paging.FromPaging(2, 10);
        
        paging.Page.Should().Be(2);
        paging.PageSize.Should().Be(10);
        paging.Skip.Should().Be(20);
        paging.Take.Should().Be(10);
    }
    
    [Test]
    public void FromSkipTake() {
        var paging = Paging.FromSkipTake(20, 10);
        
        paging.Page.Should().Be(2);
        paging.PageSize.Should().Be(10);
        paging.Skip.Should().Be(20);
        paging.Take.Should().Be(10);
    }

    [Test]
    public void SerializeAndDeserialize() {
        var paging = Paging.FromPaging(2, 10);
        var json = JsonSerializer.Serialize(paging);
        var paging2 = JsonSerializer.Deserialize<Paging>(json);

        paging2.Should().NotBeNull();
        paging2!.Skip.Should().Be(paging.Skip);
        paging2!.Take.Should().Be(paging.Take);
        paging2!.Page.Should().Be(paging.Page);
        paging2!.PageSize.Should().Be(paging.PageSize);
    }

    [Test]
    public void ApplyPagination() {
        var samples = Enumerable.Range(0, 100).ToList();
        
        var paging = Paging.FromPaging(2, 10);
        
        var result = samples.Paging(paging).ToList();
        
        result.Count.Should().Be(10);
        result.Should().BeEquivalentTo(samples.Skip(20).Take(10));
    }
}

