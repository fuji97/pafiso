using System.Collections;

namespace Pafiso.Enumerables;

public class PagedEnumerable<T>(IEnumerable<T> countQuery, IEnumerable<T> entriesQuery) : IEnumerable<T> {
    private IEnumerable<T> CountQuery { get; init; } = countQuery;
    private IEnumerable<T> EntriesQuery { get; init; } = entriesQuery;

    public PagedList<T> ToPagedList() {
        return new PagedList<T>() {
            TotalEntries = CountQuery.Count(),
            Entries = EntriesQuery.ToList()
        };
    }

    public IEnumerator<T> GetEnumerator() {
        return EntriesQuery.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return ((IEnumerable)EntriesQuery).GetEnumerator();
    }
}
