using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using Pafiso.Util;
using Shouldly;

namespace Pafiso.Tests;

public class PagingTest {
    [Test]
    public void FromPaging() {
        var paging = Paging.FromPaging(2, 10);

        paging.Page.ShouldBe(2);
        paging.PageSize.ShouldBe(10);
        paging.Skip.ShouldBe(20);
        paging.Take.ShouldBe(10);
    }

    [Test]
    public void FromSkipTake() {
        var paging = Paging.FromSkipTake(20, 10);

        paging.Page.ShouldBe(2);
        paging.PageSize.ShouldBe(10);
        paging.Skip.ShouldBe(20);
        paging.Take.ShouldBe(10);
    }

    [Test]
    public void SerializeAndDeserialize() {
        var paging = Paging.FromPaging(2, 10);
        var json = JsonSerializer.Serialize(paging);
        var paging2 = JsonSerializer.Deserialize<Paging>(json);

        paging2.ShouldNotBeNull();
        paging2!.Skip.ShouldBe(paging.Skip);
        paging2!.Take.ShouldBe(paging.Take);
        paging2!.Page.ShouldBe(paging.Page);
        paging2!.PageSize.ShouldBe(paging.PageSize);
    }

    [Test]
    public void ApplyPagination() {
        var samples = Enumerable.Range(0, 100).ToList();

        var paging = Paging.FromPaging(2, 10);

        var result = samples.Paging(paging).ToList();

        result.Count.ShouldBe(10);
        result.ShouldBe(samples.Skip(20).Take(10));
    }
}
