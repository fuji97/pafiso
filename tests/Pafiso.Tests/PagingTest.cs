using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using Pafiso.Util;

namespace Pafiso.Tests; 

public class PagingTest {
    [Test]
    public void FromPaging() {
        var paging = Paging.FromPaging(3, 10);
        
        Assert.AreEqual(3, paging.Page);
        Assert.AreEqual(10, paging.PageSize);
        Assert.AreEqual(20, paging.Skip);
        Assert.AreEqual(10, paging.Take);
    }
    
    [Test]
    public void FromSkipTake() {
        var paging = Paging.FromSkipTake(20, 10);
        
        Assert.AreEqual(3, paging.Page);
        Assert.AreEqual(10, paging.PageSize);
        Assert.AreEqual(20, paging.Skip);
        Assert.AreEqual(10, paging.Take);
    }

    [Test]
    public void SerializeAndDeserialize() {
        var paging = Paging.FromPaging(3, 10);
        var json = JsonSerializer.Serialize(paging);
        var paging2 = JsonSerializer.Deserialize<Paging>(json);
        
        Assert.AreEqual(paging.Skip, paging2.Skip);
        Assert.AreEqual(paging.Take, paging2.Take);
        Assert.AreEqual(paging.Page, paging2.Page);
        Assert.AreEqual(paging.PageSize, paging2.PageSize);
    }

    [Test]
    public void ApplyPagination() {
        var samples = Enumerable.Range(0, 100).ToList();
        
        var paging = Paging.FromPaging(3, 10);
        
        var result = samples.Paging(paging).ToList();
        
        Assert.AreEqual(10, result.Count);
        Assert.That(result, Is.EquivalentTo(samples.Skip(20).Take(10)));
    }
}